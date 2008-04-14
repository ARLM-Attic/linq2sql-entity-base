using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Reflection;

namespace LINQEntityBaseExample
{
    /// <summary>
    /// This object should be used as a superclass for LINQ to SQL Entities
    /// which are disconnected and need change tracking capabilties.
    /// 
    /// To add this class as a superclass for all entities in a DataContext
    /// Edit the dbml file for the DataContext and add in the "<Database>" xml element
    /// the "EntityBase" xml attribute, then save the dbml file, right click the dbml file in
    /// the VS project and select "Run Custom Tool".
    /// 
    /// e.g. EntityBase="LINQEntityBaseExample.LINQEntityBase"
    /// 
    /// Note, for this to work Child entities should have property a RowVersion/Timestamp field (can be named anything)
    /// </summary>    
    [DataContract()]
    //[KnownType(typeof(Order))]
    public abstract class LINQEntityBase
    {
        #region constructor

        /// <summary>
        /// Default constructor, always called as long as child entity was created with default constructure aswell.
        /// </summary>
        protected LINQEntityBase()
        {
            _entityGUID = Guid.NewGuid().ToString(); //a unique identifier for the entity

            Init();
            FindImportantProperties();
            BindToEntityEvents();           
        }

        private void Init()
        {
            _isModified = false;
            _isDeleted = false;
            _hasChangeTrackingRoot = false;
            _entityAssociationProperties = new Dictionary<string, PropertyInfo>();
            _entityAssociationFKProperties = new Dictionary<string, PropertyInfo>();
            _entityTree = new EntityTree(this, _entityAssociationProperties);
        }

        #endregion constructor

        #region private_members

        private bool _isModified; //whether or not a property has been changed
        private bool _isDeleted; //indicates if the record should be deleted        
        private bool _hasChangeTrackingRoot; //indicates if the record is being change tracked
        private string _entityGUID; //a unique identifier for the entity
        private PropertyInfo _entityVersionProperty; // stores the property info for the row stamp field.
        private Dictionary<string, PropertyInfo> _entityAssociationProperties; // stores the property info for associations
        private Dictionary<string, PropertyInfo> _entityAssociationFKProperties; // stores the property info for foreingKey associations
        private EntityTree _entityTree; //used to hold the private class that allows entity Tree to be enumerated
        private List<LINQEntityBase> _changeTrackingReferences; //holds a list of all entities, regardless of their state for the purpose of tracking changes.
        
        /// <summary>
        /// This method binds to the events of the entity that are required.
        /// </summary>
        private void BindToEntityEvents()
        {
            INotifyPropertyChanged childEntityChanged;
            childEntityChanged = (INotifyPropertyChanged)this;

            // bind the IsModified event, so when a property is changed the base class is aware of it.
            childEntityChanged.PropertyChanged +=new PropertyChangedEventHandler(PropertyChanged);
            
        }

        /// <summary>
        /// Loops through the available properties on the class and gets the rowVersion and association properties
        /// </summary>
        private void FindImportantProperties()
        {
            ColumnAttribute columnAttribute;
            AssociationAttribute assocAttribute;

            foreach (PropertyInfo propInfo in this.GetType().GetProperties())
            {
                // check it's an association attribute first
                assocAttribute = (AssociationAttribute)Attribute.GetCustomAttribute(propInfo, typeof(AssociationAttribute), false);

                // if it is an association attribute
                if (assocAttribute != null)
                {
                    // Store the FK relationships seperately (i.e. child to parent relationships);
                    if (assocAttribute.IsForeignKey != true)
                        _entityAssociationProperties.Add(propInfo.Name, propInfo);
                    else
                        _entityAssociationFKProperties.Add(propInfo.Name, propInfo);

                }
                else if (_entityVersionProperty == null)
                {
                    // it wasn't an association, check to see it's a RowVersion (if we haven't already found it).
                    columnAttribute = (ColumnAttribute)Attribute.GetCustomAttribute(propInfo, typeof(ColumnAttribute), false);
                    if (columnAttribute != null && columnAttribute.IsVersion == true)
                    {
                        _entityVersionProperty = propInfo;
                    }
                }
            }

            // By the end of this process, if we haven't found the row version property, complain.
            if (_entityVersionProperty == null)
                throw new ApplicationException("No TimeStamp column Defined, you must define one column where the TimeStamp=True in your LINQ to SQL entity.");
        }

        /// <summary>
        /// Handles the property changed event sent from child object.
        /// </summary>
        /// <param name="sender">child object</param>
        /// <param name="e">Property Changed arguements</param>
        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyInfo propInfo = null;
            
            // if the property being changed is the timestamp field
            // assume this property is being set after a submit changes.
            if (e.PropertyName == _entityVersionProperty.Name)
            {
                // the object can be assumed to be no longer in a modified or deleted state
                // so reset the flags.  
                // NOTE: IsNew does not need to be set because this is dynamically evaluated.
                _isModified = false;
                _isDeleted = false;
            }
            else
            {
                // Check if it's new and it's an association. 
                // If the entity is new, it's not modified, so look into change tracking flag
                // If it's an association parent->child (non FK) then ignore
                // If it's an association child->parent (is FK) and value has been changed to null then it's a removal
                // If it's none of the above it's modified.
                if (!IsNew)
                {
                    // only go into this section if it's change tracked
                    if (this.HasChangeTrackingRoot == true)
                    {
                        if (!_entityAssociationProperties.ContainsKey(e.PropertyName))
                        {
                            if (_entityAssociationFKProperties.ContainsKey(e.PropertyName))
                            {
                                if (_entityAssociationFKProperties.TryGetValue(e.PropertyName, out propInfo))
                                {
                                    if ((propInfo != null) && (propInfo.GetValue(this, null) == null))
                                    {
                                        _isDeleted = true;
                                    }
                                }
                            }
                            else
                            {
                                _isModified = true;
                            }
                        }
                    }
                }
                else
                {
                    // only go into this section if it's not changed tracked yet.
                    if (this.HasChangeTrackingRoot == false)
                    {
                        // Check to see if the parent objec is change tracked
                        // If there is, tell this new object it's tracked
                        if (_entityAssociationFKProperties.ContainsKey(e.PropertyName))
                        {
                            if (_entityAssociationFKProperties.TryGetValue(e.PropertyName, out propInfo))
                            {
                                if (propInfo != null)
                                {
                                    LINQEntityBase entity = (LINQEntityBase)propInfo.GetValue(this, null);

                                    if (entity.HasChangeTrackingRoot == true)
                                    {
                                        this.HasChangeTrackingRoot = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets whether this entity has a change tracking root.
        /// </summary>
        [DataMember(Order = 4)]
        private bool HasChangeTrackingRoot
        {
            get
            {
                return _hasChangeTrackingRoot;
            }
            set
            {
                _hasChangeTrackingRoot = value;
            }
        }

        /// <summary>
        /// Gets/Sets the entities which have been deleted.
        /// (Data Contract Serialization Only)
        /// </summary>
        [DataMember(Order = 5)]
        private List<LINQEntityBase> DeletedEntities
        {
            get
            {
                if (_changeTrackingReferences != null)
                {
                    List<LINQEntityBase> entities = new List<LINQEntityBase>();
                    entities.AddRange(_changeTrackingReferences.Where(e => e.IsDeleted == true));
                    return entities;
                }
                else
	            {
                    return null;
	            }           
            }

            set
            {
                _changeTrackingReferences = value;
            }
        }

        /// <summary>
        /// When starting deserialization, call this method to make sure that 
        /// private variables are setup.
        /// </summary>
        /// <param name="sc"></param>
        [OnDeserializing()]
        private void BeforeDeserializing(System.Runtime.Serialization.StreamingContext sc)
        {
            Init();
        }

        /// <summary>
        /// Called at the final stage of serialization to make sure that the interal
        /// change tracking references are correct.
        /// </summary> 
        [OnDeserialized()]
        private void AfterDeserialized(System.Runtime.Serialization.StreamingContext sc)
        {
            // if it's the root, _changeTrackingReferences will contain deleted references
            // and we need to build a complete list of all objects - not just the deleted ones
            if(_changeTrackingReferences != null)
            {
                _changeTrackingReferences = this.ToEntityTree();
            }

            FindImportantProperties();
            BindToEntityEvents();  
        }



        #endregion private_members
        
        #region public_members


        /// <summary>
        /// Returns an ID that is unique for this object.
        /// </summary>
        [DataMember(Order = 1)]
        public string LINQEntityGUID
        {
            get 
            { 
                return _entityGUID; 
            }
            private set 
            { 
                _entityGUID = value; 
            }
        }

        /// <summary>
        /// Returns if the entity is new or not
        /// </summary>
        public bool IsNew
        {
            get
            {
                // If the timestamp is null then it's new.
                return _entityVersionProperty.GetValue(this, null) == null;
            }
        }

        /// <summary>
        /// Returns if the property is modified or allows caller to set if the
        /// object is modified.
        /// </summary> 
        [DataMember(Order = 2)]
        public bool IsModified
        {
            get
            {
                // If it's been marked as modified, and it's not new then it's considered modified.
                return _isModified == true && IsNew == false;
            }
            set
            {
                // If the caller want's to specifically attempt to modify for
                // some reason or we are re-hydrating the object from serialization
                // allow _isModified to be set manually
                _isModified = value;
            }
        }

        /// <summary>
        /// Returns/Sets if the object has been marked for deletion.
        /// </summary>
        [DataMember(Order = 3)]
        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
            private set
            {
                _isDeleted = value;
            }
        }

        /// <summary>
        /// This method flattens the hierachy of objects into a single list that can be queried by linq
        /// </summary>
        /// <returns></returns>
        public List<LINQEntityBase> ToEntityTree()
        {            
            // Deleted records won't show up in entity tree
            // So include these when returning the results

            List<LINQEntityBase> entities;

            entities = (from t in _entityTree                    
                       select t).ToList();

            if (_changeTrackingReferences != null)
            {
                entities.AddRange(_changeTrackingReferences.Where( e => e.IsDeleted == true));
            }

            return entities;
        }


        /// <summary>
        /// Sets the current entity as the root for change tracking.
        /// </summary>
        public void SetAsChangeTrackingRoot()
        {
            // Throw an exception if this object is already being change tracked
            if (this._hasChangeTrackingRoot && this._changeTrackingReferences == null)
                throw new ApplicationException("This object is already in a Change Tracking Tree and cannot be the root.");

            // This is the root object, so grab a list of all the references and keep for later.
            // We need this, so that we can track entity deletions.
            _changeTrackingReferences = this._entityTree.ToList();
            foreach (LINQEntityBase entity in _changeTrackingReferences)
            {
                entity.HasChangeTrackingRoot = true;
            }
        }

        /// <summary>
        /// Syncronises this EntityBase and all sub objects
        /// with a data context. Assumes you want cascade deletes.
        /// </summary>
        /// <param name="targetDataContext">The data context that will apply the changes</param>
        public void SynchroniseWithDataContext(DataContext targetDataContext)
        {
            SynchroniseWithDataContext(targetDataContext, true);
        }

        /// <summary>
        /// Syncronises this EntityBase and all sub objects
        /// with a data context.
        /// </summary>
        /// <param name="targetDataContext">The data context that will apply the changes</param>
        /// <param name="cascadeDelete">Whether or not casade deletes is allowed</param>
        public void SynchroniseWithDataContext(DataContext targetDataContext, bool cascadeDelete)
        {
            // Before doing anything, check to make sure that the new datacontext
            // doesn't try any deferred (lazy) loading
            if (targetDataContext.DeferredLoadingEnabled == true)
                throw new ApplicationException("Syncronisation requires that the Deferred loading is disabled on the Target DataContext");

            // Also Make sure this entity is the change tracking root
            if (this._changeTrackingReferences == null)
                throw new ApplicationException("You cannot syncronise an entity that is not the change tracking root");

            List<LINQEntityBase> entities = this.ToEntityTree().ToList();          

            foreach (LINQEntityBase entity in entities)
            {
                if (!entity.IsNew && !entity.IsModified && !entity.IsDeleted)
                    targetDataContext.GetTable(entity.GetType()).Attach(entity, false);
                else if (entity.IsNew)
                    targetDataContext.GetTable(entity.GetType()).InsertOnSubmit(entity);
                else if (entity.IsModified)
                    targetDataContext.GetTable(entity.GetType()).Attach(entity, true);

                if (entity.IsDeleted)
                {
                    // Check to see if cascading deletes is allowed
                    if (cascadeDelete)
                    {
                        // Grab the entity tree and reverse it so that this entity is deleted last
                        List<LINQEntityBase> entityTreeReversed = entity.ToEntityTree();
                        entityTreeReversed.Reverse();

                        // Cascade delete children and then this object
                        foreach (LINQEntityBase toDelete in entityTreeReversed)
                        {
                            targetDataContext.GetTable(toDelete.GetType()).Attach(toDelete);
                            targetDataContext.GetTable(toDelete.GetType()).DeleteOnSubmit(toDelete);
                        }
                    }
                    else
                    {
                        targetDataContext.GetTable(entity.GetType()).Attach(entity);
                        targetDataContext.GetTable(entity.GetType()).DeleteOnSubmit(entity);
                    }
                    
                }

            }

            // Reset this entity as the change tracking root, getting a new copy of all objects
            this.SetAsChangeTrackingRoot();
        }

        #endregion public_members

        #region private_classes

        /// <summary>
        /// This class is used internally to implement IEnumerable, so that the Tree can
        /// be enumerated by LINQ queries.
        /// </summary>
        private class EntityTree : IEnumerable<LINQEntityBase>
        {
            private Dictionary<string, PropertyInfo> _entityAssociationProperties;
            private LINQEntityBase _entityRoot;

            public EntityTree(LINQEntityBase EntityRoot, Dictionary<string, PropertyInfo> EntityAssociationProperties)
            {
                _entityRoot = EntityRoot;
                _entityAssociationProperties = EntityAssociationProperties;
            }

            // implement the GetEnumerator Type
            public IEnumerator<LINQEntityBase> GetEnumerator()
            {
                // return the current object
                yield return _entityRoot;

                // return the children (using reflection)
                foreach (PropertyInfo propInfo in _entityAssociationProperties.Values)
                {
                    // Is it an EntitySet<> ?
                    if (propInfo.PropertyType.IsGenericType && propInfo.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>))
                    {
                        // It's an EntitySet<> so lets grab the value, loop through each value and
                        // return each value as an EntityBase.
                        IEnumerator entityList = (propInfo.GetValue(_entityRoot, null) as IEnumerable).GetEnumerator();

                        while (entityList.MoveNext() == true)
                        {
                            if (entityList.Current.GetType().IsSubclassOf(typeof(LINQEntityBase)))
                            {
                                LINQEntityBase currentEntity = (LINQEntityBase)entityList.Current;
                                foreach (LINQEntityBase subEntity in currentEntity.ToEntityTree())
                                {
                                    yield return subEntity;
                                }
                            }
                        }
                    }
                    else if (propInfo.PropertyType.IsSubclassOf(typeof(LINQEntityBase)))
                    {
                        // ignore the property if it's null
                        if (propInfo.GetValue(_entityRoot, null) != null)
                        {
                            //Ask for these children for their section of the tree.
                            foreach (LINQEntityBase subEntity in (propInfo.GetValue(_entityRoot, null) as LINQEntityBase).ToEntityTree())
                            {
                                yield return subEntity;
                            }

                        }
                    }
                }

            }

            // implement the GetEnumerator type
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        #endregion


    }

}
