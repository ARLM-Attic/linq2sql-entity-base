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
    public abstract class LINQEntityBase
    {
        #region constructor

        /// <summary>
        /// Default constructor, always called as long as child entity was created with default constructure aswell.
        /// </summary>
        protected LINQEntityBase()
        {
            FindImportantProperties();
            BindToEntityEvents();
            _entityTree = new EntityTree(this, _entityAssociationProperties);
        }

        #endregion constructor

        #region private_members

        private bool _isModified = false; //whether or not a property has been changed
        private bool _isDeleted = false; //indicates if the record should be deleted        
        private string _entityGUID = Guid.NewGuid().ToString(); //a unique identifier for the entity
        private PropertyInfo _entityVersionProperty; // stores the property info for the row stamp field.
        private Dictionary<string, PropertyInfo> _entityAssociationProperties = new Dictionary<string, PropertyInfo>(); // stores the property info for associations
        private Dictionary<string, PropertyInfo> _entityAssociationFKProperties = new Dictionary<string, PropertyInfo>(); // stores the property info for foreingKey associations
        private EntityTree _entityTree; //used to hold the private class that allows entity Tree to be enumerated

        /// <summary>
        /// Returns an ID that is unique for this object.
        /// </summary>
        public string LINQEntityGUID
        {
            get { return _entityGUID; }
        }

        /// <summary>
        /// This method binds to the events of the entity that are required.
        /// </summary>
        private void BindToEntityEvents()
        {
            INotifyPropertyChanged childEntity;
            childEntity = (INotifyPropertyChanged)this;

            // bind the IsModified event, so when a property is changed the base class is aware of it.
            childEntity.PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);

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
            // if the property being changed is the timestamp field
            // assume this property is being set after a submit changes.
            if (e.PropertyName == _entityVersionProperty.Name)
            {
                // the object can be assumed to be no longer in a modified or deleted state
                // so reset the flag.  IsNew does not need to be set because this is dynamically evaluated.
                _isModified = false;
                _isDeleted = false;
            }
            else
            {
                // Check if it's new and it's an association. 
                // If the entity is new, it's new not modified so ignore.
                // If the property is an association then mark the entity as modified.
                if (!IsNew &&
                    !_entityAssociationProperties.ContainsKey(e.PropertyName) &&
                    !_entityAssociationFKProperties.ContainsKey(e.PropertyName))
                {
                    _isModified = true;
                }
            }
        }


        #endregion private_members

        #region public_members

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
        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
            set
            {
                // Allow a user to specifically set that an object is deleted.
                // as this object doesn't delete deleted objects.
                _isDeleted = value;
            }
        }

        /// <summary>
        /// This method flattens the hierachy of objects into a single list that can be queried by linq
        /// </summary>
        /// <returns></returns>
        public IEnumerable<LINQEntityBase> ToEntityTree()
        {
            return (from t in _entityTree
                    select t);
        }


        /// <summary>
        /// Syncronises this EntityBase and all sub objects
        /// with a data context.
        /// </summary>
        /// <param name="targetDataContext">The data context that will apply the changes</param>
        public void SynchroniseWithDataContext(DataContext targetDataContext)
        {
            // Before doing anything, check to make sure that the new datacontext
            // doesn't try any deferred (lazy) loading
            if(targetDataContext.DeferredLoadingEnabled == true)
                throw new ApplicationException("Syncronisation requires that the Deferred loading is disabled on the Target DataContext");


            foreach (LINQEntityBase entity in this.ToEntityTree())
            {
                if (!entity.IsNew && !entity.IsModified)
                    targetDataContext.GetTable(entity.GetType()).Attach(entity, false);
                else if (entity.IsNew)
                    targetDataContext.GetTable(entity.GetType()).InsertOnSubmit(entity);
                else if (entity.IsModified)
                    targetDataContext.GetTable(entity.GetType()).Attach(entity, true);

                if (entity.IsDeleted)
                    targetDataContext.GetTable(entity.GetType()).DeleteOnSubmit(entity);
            }
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
                        //Ask for these children for their section of the tree.
                        foreach (LINQEntityBase subEntity in (propInfo.GetValue(_entityRoot, null) as LINQEntityBase).ToEntityTree())
                        {
                            yield return subEntity;
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
