﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Reflection;


namespace LINQEntityBaseExampleData
{

    /// <summary>
    /// Indicates the entity state.
    /// NotTracked = this object is not change tracked.
    /// Original = persisted, but unmodified.
    /// New = Record to be inserted.
    /// Modified = Record to be updated.
    /// Deleted = Record to be deleted.
    /// </summary>
    public enum EntityState
    {
        NotTracked,
        Original,
        New,
        Modified,
        Deleted,
    }

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
    [KnownType("GetKnownTypes")]
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
            _entityState = EntityState.NotTracked;
            _isKeepOriginal = false;

            _entityAssociationProperties = new Dictionary<string, PropertyInfo>();
            _entityAssociationFKProperties = new Dictionary<string, PropertyInfo>();
            _entityTree = new EntityTree(this, _entityAssociationProperties);
        }

        #endregion constructor

        #region private_members

        private EntityState _entityState; //returns the current entity state
        private bool _isKeepOriginal; //indicates if the original record before modifications should be kept for use when syncing with DataContext later on.
        private string _entityGUID; //a unique identifier for the entity       
        private Dictionary<string, PropertyInfo> _entityAssociationProperties; // stores the property info for associations
        private Dictionary<string, PropertyInfo> _entityAssociationFKProperties; // stores the property info for foreingKey associations
        private EntityTree _entityTree; //used to hold the private class that allows entity Tree to be enumerated
        private List<LINQEntityBase> _changeTrackingReferences; //holds a list of all entities, regardless of their state for the purpose of tracking changes.
        private LINQEntityBase _originalEntityValue; // holds the original entity values before modification
        private LINQEntityBase _originalEntityValueTemp; // temporarily holds the original entity value until we no it's a true modification.
        /// <summary>
        /// This method binds to the events of the entity that are required.
        /// </summary>
        private void BindToEntityEvents()
        {
            INotifyPropertyChanged childEntityChanged;
            childEntityChanged = (INotifyPropertyChanged)this;

            INotifyPropertyChanging childEntityChanging;
            childEntityChanging = (INotifyPropertyChanging)this;

            // bind the events, so when a property is changed the base class is aware of it.
            childEntityChanged.PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);
            childEntityChanging.PropertyChanging += new PropertyChangingEventHandler(PropertyChanging);

            
        }

        /// <summary>
        /// Loops through the available properties on the class and gets the rowVersion and association properties
        /// </summary>
        private void FindImportantProperties()
        {            
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
            }
        }


        /// <summary>
        /// Handles the property changing event sent from child object.
        /// </summary>
        /// <param name="sender">child object</param>
        /// <param name="e">Property Changing arguements</param>
        private void PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            // If it's a change tracked object thats in "Original" state
            // grab a copy of the object incase it's going to be modified
            if (this.LINQEntityState == EntityState.Original && LINQEntityKeepOriginal == true && LINQEntityOriginalValue == null)
            {
                _originalEntityValueTemp = ShallowCopy(this);
            }
        }

        /// <summary>
        /// Handles the property changed event sent from child object.
        /// </summary>
        /// <param name="sender">child object</param>
        /// <param name="e">Property Changed arguements</param>
        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyInfo propInfo = null;

            // if this object isn't change tracked yet, but it's parent
            // is, this means it's a new object
            if (this.LINQEntityState == EntityState.NotTracked)
            {
                // Check to see if the parent object is change tracked
                // If there is, set the new flag, and tell this new object it's tracked
                if (_entityAssociationFKProperties.ContainsKey(e.PropertyName))
                {
                    if (_entityAssociationFKProperties.TryGetValue(e.PropertyName, out propInfo))
                    {
                        if (propInfo != null)
                        {
                            LINQEntityBase parentEntity = (LINQEntityBase)propInfo.GetValue(this, null);

                            if (parentEntity.LINQEntityState != EntityState.NotTracked)
                            {
                                this.LINQEntityState = EntityState.New;
                            }
                        }
                    }
                }
            }
            

            //if the object is not new....
            if (LINQEntityState != EntityState.New)
            {
                // only go into this section if it's change tracked
                if (this.LINQEntityState != EntityState.NotTracked)
                {
                    if (!_entityAssociationProperties.ContainsKey(e.PropertyName))
                    {
                        if (_entityAssociationFKProperties.ContainsKey(e.PropertyName))
                        {
                            if (_entityAssociationFKProperties.TryGetValue(e.PropertyName, out propInfo))
                            {
                                if ((propInfo != null) && (propInfo.GetValue(this, null) == null))
                                {
                                    LINQEntityState = EntityState.Deleted;
                                }
                            }
                        }
                        else
                        {
                            // if the object isn't already modified
                            // set it as modified

                            if (LINQEntityState != EntityState.Modified)
                            {
                                this._originalEntityValue = this._originalEntityValueTemp;                                
                                LINQEntityState = EntityState.Modified;
                            }
                        }
                    }
                }
            }

            this._originalEntityValueTemp = null;
        }

        /// <summary>
        /// Gets/Sets whether this entity has a change tracking root.
        /// </summary>
        [DataMember(Order = 3)]
        private bool LINQEntityKeepOriginal
        {
            get
            {
                return _isKeepOriginal;
            }
            set
            {
                _isKeepOriginal = value;
            }
        }

        /// <summary>
        /// Gets/Sets the original entity value before any changes
        /// </summary>
        [DataMember(Order = 4)]
        private LINQEntityBase LINQEntityOriginalValue
        {
            get
            {
                return _originalEntityValue;
            }
            set
            {
                _originalEntityValue = value;
            }
        }

        /// <summary>
        /// Gets/Sets the entities which have been deleted.
        /// (Data Contract Serialization Only)
        /// </summary>
        [DataMember(Order = 5)]
        private List<LINQEntityBase> LINQEntityDeletedEntities
        {
            get
            {
                // if in the root object, get all deleted records 
                // except for root object if marked as deleted
                if (_changeTrackingReferences != null)
                {
                    List<LINQEntityBase> entities = new List<LINQEntityBase>();
                    entities.AddRange(_changeTrackingReferences.Where(e => e.LINQEntityState == EntityState.Deleted && e != this));
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
        /// Gets the list of Known Types
        /// </summary>
        /// <returns></returns>        
        private static List<Type> GetKnownTypes()
        {
            return (from a in Assembly.GetExecutingAssembly().GetTypes()
                   where a.IsSubclassOf(typeof(LINQEntityBase))
                   select a).ToList();
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
        /// <summary>
        /// Make a shallow copy of column values without copying references of the source entity
        /// </summary>
        /// <param name="source">the source entity that will have it's values copied</param>
        /// <returns></returns>
        private LINQEntityBase ShallowCopy(LINQEntityBase source)
        {
            PropertyInfo[] sourcePropInfos = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] destinationPropInfos = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            // create an object to copy values into
            Type entityType = source.GetType();
            LINQEntityBase destination;
            destination = Activator.CreateInstance(entityType) as LINQEntityBase;

            foreach (PropertyInfo sourcePropInfo in sourcePropInfos)
            {
                if (Attribute.GetCustomAttribute(sourcePropInfo, typeof(ColumnAttribute), false) != null)
                {
                    PropertyInfo destPropInfo = destinationPropInfos.Where(pi => pi.Name == sourcePropInfo.Name).First();
                    destPropInfo.SetValue(destination, sourcePropInfo.GetValue(source, null), null);
                }
            }

            return destination;
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

        [DataMember(Order = 2)]
        public EntityState LINQEntityState
        {
            get
            {
                return _entityState;
            }
            private set
            {
                _entityState = value;
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
                entities.AddRange(LINQEntityDeletedEntities);
            }

            return entities;
        }

        /// <summary>
        /// Sets the current entity as the root for change tracking.
        /// Assumes object is in original state (unmodified)
        /// Does not keep original values.
        /// </summary>
        public void SetAsChangeTrackingRoot()
        {
            SetAsChangeTrackingRoot( EntityState.Original, false);
        }

        /// <summary>
        /// Sets the current entity as the root for change tracking.
        /// Assumes object is in original state (unmodified)
        /// </summary>
        /// <param name="OnModifyKeepOriginal">If modified, original entity state is kept for attachment to context later on.</param>
        public void SetAsChangeTrackingRoot(bool onModifyKeepOriginal)
        {
            SetAsChangeTrackingRoot(EntityState.Original, onModifyKeepOriginal);
        }

        /// <summary>
        /// Sets the current entity as the root for change tracking.
        /// Does not keep original values.
        /// </summary>
        /// <param name="initialEntityState">The initial state of the root entity</param>
        public void SetAsChangeTrackingRoot(EntityState initialEntityState)
        {
            SetAsChangeTrackingRoot(initialEntityState, false);
        }

        /// <summary>
        /// Sets the current entity as the root for change tracking and resets all the values of all entities.
        /// </summary>
        /// <param name="initialEntityState">The initial state of the root entity</param>
        /// <param name="onModifyKeepOriginal">If modified, original entity state is kept for attachment to context later on.</param>
        public void SetAsChangeTrackingRoot(EntityState initialEntityState, bool onModifyKeepOriginal)
        {
            // Throw an exception if this object is already being change tracked
            if (this.LINQEntityState != EntityState.NotTracked && this._changeTrackingReferences == null)
                throw new ApplicationException("This entity is already being Change Tracked and cannot be the root.");
            
            // Throw an exception if "IsModified" is passed in - this is not allowed
            if (initialEntityState == EntityState.Modified)
                throw new ApplicationException("An Entity cannot be set as the Change Tracking Root whilst modified.  Instead, Set as Change Tracking root and then modify the entity.");

            // This is the root object, so grab a list of all the references and keep for later.
            // We need this, so that we can track entity deletions.
            _changeTrackingReferences = this._entityTree.ToList();

            // Reset all the change tracked object states
            foreach (LINQEntityBase entity in _changeTrackingReferences)
            {
                // if initial state is deleted, only set the root object to deleted
                if (initialEntityState == EntityState.Deleted)
                    if (this == entity)
                        entity.LINQEntityState = EntityState.Deleted;
                    else
                        entity.LINQEntityState = EntityState.Original;
                else
                    entity.LINQEntityState = initialEntityState;

                entity.LINQEntityOriginalValue = null;
                entity.LINQEntityKeepOriginal = onModifyKeepOriginal;
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
                if (entity.LINQEntityState == EntityState.Original)
                {
                    targetDataContext.GetTable(entity.GetType()).Attach(entity, false);
                }
                else if (entity.LINQEntityState == EntityState.New)
                {
                    targetDataContext.GetTable(entity.GetType()).InsertOnSubmit(entity);
                }
                else if (entity.LINQEntityState == EntityState.Modified)
                {
                    if (this.LINQEntityKeepOriginal == true)
                        targetDataContext.GetTable(entity.GetType()).Attach(entity, entity.LINQEntityOriginalValue);
                    else
                        targetDataContext.GetTable(entity.GetType()).Attach(entity, true);
                }

                if (entity.LINQEntityState == EntityState.Deleted)
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

                    // if this is the root object, there's no need to do more processing 
                    // so just quit the loop
                    if (this == entity)
                        break;                    
                }
            }

            // Reset this entity as the change tracking root, getting a new copy of all objects
            this.SetAsChangeTrackingRoot(this.LINQEntityKeepOriginal);
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