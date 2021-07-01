using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;

using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Basics;
using Slb.Ocean.Petrel.DomainObject.Well;

namespace TNIPI.Misc
{
    /// <summary>
    /// This class contains all the methods and subclasses of the CopyAttributes.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    public class CopyAttributes : Workstep<CopyAttributes.Arguments>, IPresentation, IDescriptionSource
    {
        /// <summary>
        /// This method does the work of the process.
        /// </summary>
        /// <param name="argumentPackage">the arguments to use during the process</param>
        protected override void InvokeSimpleCore(Arguments argumentPackage)
        {
            // TODO: finish the Invoke method implementation
            DateTime start = DateTime.Now;
            try
            {
                BoreholeCollection bhColl = argumentPackage.WellCollection;
                PropertyBase bhPropBase = argumentPackage.WellAttribute;
                Surface ifc = argumentPackage.Horizon;
                DictionaryBoreholeProperty filter = argumentPackage.Filter;

                if (bhColl == null)
                {
                    MessageBox.Show("Well collection can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (bhPropBase == null)
                {
                    MessageBox.Show("Well attribute can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if(!(bhPropBase is DictionaryBoreholeProperty) && !(bhPropBase is BoreholeProperty))
                {
                    MessageBox.Show("Well attribute is invalid", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (ifc == null)
                {
                    MessageBox.Show("Horizon can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (filter != null)
                {
                    if (filter.DataType != typeof(int))
                    {
                        MessageBox.Show("Filter must be discrete", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                List<Borehole> bhList = new List<Borehole>();
                FillBoreholeList(bhColl, bhList);

                if (bhList.Count == 0)
                {
                    MessageBox.Show("Wells not found", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                
                if(ifc.MarkerCount == 0)
                {
                    MessageBox.Show("No well tops in this horizon", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                MarkerCollection markerColl = null;
                foreach (Marker marker in ifc.Markers)
                {
                    markerColl = marker.MarkerCollection;
                    break;
                }

                WellPointPropertyCollection wpPropColl = markerColl.MarkerPropertyCollection;
                PropertyBase wpProp = GetWellPointDictionaryProperty(bhPropBase.Name, wpPropColl);

                using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
                {
                    if (bhPropBase is DictionaryBoreholeProperty)
                    {
                        if (wpProp == null)
                        {
                            trans.Lock(wpPropColl);
                            if (bhPropBase.DataType.Equals(typeof(int)))
                            {
                                DictionaryBoreholeProperty bhProp = bhPropBase as DictionaryBoreholeProperty;
                                wpProp = wpPropColl.CreateDictionaryProperty(bhProp.DictionaryPropertyVersion, bhProp.Name);
                            }
                            else
                                wpProp = wpPropColl.CreateDictionaryProperty(bhPropBase.DataType, bhPropBase.Name);
                        }
                    }

                    if (bhPropBase is BoreholeProperty)
                    {
                        if (wpProp == null)
                        {
                            trans.Lock(wpPropColl);
                            if (bhPropBase.DataType.Equals(typeof(double)) || bhPropBase.DataType.Equals(typeof(float)))
                            {
                                BoreholeProperty bhProp = bhPropBase as BoreholeProperty;
                                wpProp = wpPropColl.CreateProperty(bhProp.PropertyVersion, bhProp.Name);
                            }
                            else
                                wpProp = wpPropColl.CreateProperty(bhPropBase.DataType, bhPropBase.Name);
                        }
                    }

                    trans.Commit();
                }

                Type CopyAttributesType = typeof(CopyAttributes);
                MethodInfo GenericGetBoreholePropertyMethodInfo = CopyAttributesType.GetMethod("GetBoreholeProperty");
                MethodInfo ConstructedGetBoreholePropertyMethodInfo = GenericGetBoreholePropertyMethodInfo.MakeGenericMethod(bhPropBase.DataType);
                /*
                Type GenericListType = typeof(List<>);
                Type ConstructedListType = GenericListType.MakeGenericType(wpProp.DataType);
                ConstructorInfo ListTypeConstructor = ConstructedListType.GetConstructor(new Type[] { typeof(int) });
                MethodInfo ConstructedAddMethodInfo = ConstructedListType.GetMethod("Add");

                Type MarkerCollectionType = typeof(MarkerCollection);
                MethodInfo GenericSetPropertyValuesMethodInfo = MarkerCollectionType.GetMethod("SetPropertyValues");
                MethodInfo ConstructedSetPropertyValuesMethodInfo = GenericSetPropertyValuesMethodInfo.MakeGenericMethod(bhPropBase.DataType);
                */
                foreach (Borehole bh in bhList)
                {
                    if (filter != null)
                    {
                        int flt = bh.PropertyAccess.GetPropertyValue<int>(filter);
                        if (flt == 0 || flt == Marker.UndefinedDictionaryPropertyValue)
                            continue;
                    }

                    int markercnt = ifc.GetMarkerCount(bh);
                    if (markercnt == 0)
                        continue;

                    using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
                    {
                        try
                        {
                            object val = ConstructedGetBoreholePropertyMethodInfo.Invoke(this, new object[] { bh, bhPropBase });
                            val = Convert.ChangeType(val, wpProp.DataType);
                            /*
                            object lst = ListTypeConstructor.Invoke(new object[] { markercnt });
                            for (int i = 0; i < markercnt; i++)
                                ConstructedAddMethodInfo.Invoke(lst, new object[] { val });
                            ConstructedSetPropertyValuesMethodInfo.Invoke(markerColl, new object[] { ifc.GetMarkers(bh), wpProp, lst });
                            */
                            foreach(Marker marker in ifc.GetMarkers(bh))
                            {
                                trans.Lock(marker);
                                SetMarkerProperty(marker, wpProp, val);
                            }
                        }
                        catch (Exception exc)
                        {
                            PetrelLogger.InfoOutputWindow(bh.Name + ": " + exc.Message);
                        }

                        trans.Commit();
                    }
                }

                DateTime end = DateTime.Now;
                PetrelLogger.InfoOutputWindow("Execution complete");
                PetrelLogger.InfoOutputWindow("Execution time: " + (end - start).ToString());
            }
            catch (Exception exc)
            {
                PetrelLogger.InfoOutputWindow(exc.Message);
                PetrelLogger.InfoOutputWindow("Execution failed");
            }
        }

        private void FillBoreholeList(BoreholeCollection bhColl, List<Borehole> bhList)
        {
            foreach (Borehole bh in bhColl)
                bhList.Add(bh);

            foreach (BoreholeCollection bhc in bhColl.BoreholeCollections)
                FillBoreholeList(bhc, bhList);
        }

        public void SetMarkerProperty(Marker marker, PropertyBase wpProp, object obj)
        {
            if (obj != null)
                marker.PropertyAccess.SetPropertyValue(wpProp, obj);
            else
                marker.PropertyAccess.SetPropertyValue(wpProp, Slb.Ocean.Data.NullObject.NullValueDefaults.GetDefaultByType(obj.GetType()));
        }

        public T GetBoreholeProperty<T>(Borehole bh, PropertyBase bhProp)
        {
            T retval = bh.PropertyAccess.GetPropertyValue<T>(bhProp);
            return retval;
        }

        private bool HasWellPointDictionaryProperty(string name, WellPointPropertyCollection wpPropColl)
        {
            bool found = false;
            foreach (DictionaryWellPointProperty wpProp in wpPropColl.DictionaryProperties)
                if (wpProp.Name == name) // && wpProp.DataType.Equals(type))
                {
                    found = true;
                    break;
                }

            return found;
        }

        private DictionaryWellPointProperty GetWellPointDictionaryProperty(string name, WellPointPropertyCollection wpPropColl)
        {
            foreach (DictionaryWellPointProperty wpProp in wpPropColl.DictionaryProperties)
                if (wpProp.Name == name) // && wpProp.DataType.Equals(type))
                    return wpProp;

            return null;
        }

        private bool HasWellPointProperty(string name, WellPointPropertyCollection wpPropColl)
        {
            bool found = false;
            foreach (WellPointProperty wpProp in wpPropColl.Properties)
                if (wpProp.Name == name) // && wpProp.DataType.Equals(type))
                {
                    found = true;
                    break;
                }

            return found;
        }

        private WellPointProperty GetWellPointProperty(string name, WellPointPropertyCollection wpPropColl)
        {
            foreach (WellPointProperty wpProp in wpPropColl.Properties)
                if (wpProp.Name == name) // && wpProp.DataType.Equals(type))
                    return wpProp;

            return null;
        }

        #region CopyArgPack implementation

        protected override void CopyArgumentPackageCore(Arguments fromArgumentPackage, Arguments toArgumentPackage)
        {
            DescribedArgumentsHelper.Copy(fromArgumentPackage, toArgumentPackage);
        }

        #endregion

        /// <summary>
        /// ArgumentPackage class for CopyAttributes.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            private Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection wellCollection;
            private Slb.Ocean.Petrel.DomainObject.Basics.PropertyBase wellAttribute;
            private Slb.Ocean.Petrel.DomainObject.Well.Surface horizon;
            private Slb.Ocean.Petrel.DomainObject.Well.DictionaryBoreholeProperty filter;

            [Description("WellCollection", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection WellCollection
            {
                internal get { return this.wellCollection; }
                set { this.wellCollection = value; }
            }

            [Description("WellAttribute", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Basics.PropertyBase WellAttribute
            {
                internal get { return this.wellAttribute; }
                set { this.wellAttribute = value; }
            }
            
            [Description("Horizon", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.Surface Horizon
            {
                internal get { return this.horizon; }
                set { this.horizon = value; }
            }
            
            [Description("Filter", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.DictionaryBoreholeProperty Filter
            {
                internal get { return this.filter; }
                set { this.filter = value; }
            }
        }
    
        #region IPresentation Members

        public event EventHandler PresentationChanged;

        public string Text
        {
            get { return Description.Name; }
        }

        public System.Drawing.Bitmap Image
        {
            get { return PetrelImages.Modules; }
        }

        #endregion

        #region IDescriptionSource Members

        /// <summary>
        /// Gets the description of the CopyAttributes
        /// </summary>
        public IDescription Description
        {
            get { return CopyAttributesDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the CopyAttributes.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class CopyAttributesDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private  static CopyAttributesDescription instance = new CopyAttributesDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static CopyAttributesDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of CopyAttributes
            /// </summary>
            public string Name
            {
                get { return "Copy attributes"; }
            }
            /// <summary>
            /// Gets the short description of CopyAttributes
            /// </summary>
            public string ShortDescription
            {
                get { return "Copy well attribute to well top attribute"; }
            }
            /// <summary>
            /// Gets the detailed description of CopyAttributes
            /// </summary>
            public string Description
            {
                get { return "If filter is set, only wells with defined non-zero filter value are considered."; }
            }

            #endregion
        }

        #endregion
    }
}