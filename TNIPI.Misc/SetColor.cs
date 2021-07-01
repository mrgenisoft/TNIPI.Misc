using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;

using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject.Well;

namespace TNIPI.Misc
{
    /// <summary>
    /// This class contains all the methods and subclasses of the SetColor.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    public class SetColor : Workstep<SetColor.Arguments>, IPresentation, IDescriptionSource
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
                Borehole refbh = argumentPackage.Borehole;
                bool setColor = argumentPackage.SetColor;
                bool setSymbol = argumentPackage.SetSymbol;
                DictionaryBoreholeProperty filter = argumentPackage.Filter;

                if (bhColl == null)
                {
                    MessageBox.Show("Well collection can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (refbh == null)
                {
                    MessageBox.Show("Reference well can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!setColor && !setSymbol)
                {
                    MessageBox.Show("Either SetColor or SetSymbol must be set", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                IBoreholePresentationFactory refbpf = CoreSystem.GetService<IBoreholePresentationFactory>(refbh); // IBoreholePresentationFactory inside generic angle brackets
                IBoreholePresentation refpres = refbpf.GetBoreholePresentation(refbh);
                Color refcolor = refpres.Color;
                WellSymbolDescription refwsd = refpres.WellSymbol;

                foreach (Borehole bh in bhList)
                {
                    if (filter != null)
                    {
                        int flt = bh.PropertyAccess.GetPropertyValue<int>(filter);
                        if (flt == 0 || flt == Marker.UndefinedDictionaryPropertyValue)
                            continue;
                    }

                    IBoreholePresentationFactory bpf = CoreSystem.GetService<IBoreholePresentationFactory>(bh); // IBoreholePresentationFactory inside generic angle brackets
                    IBoreholePresentation pres = bpf.GetBoreholePresentation(bh);

                    if (setColor)
                        pres.Color = refcolor;

                    if (setSymbol)
                        pres.WellSymbol = refwsd;
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

        #region CopyArgPack implementation

        protected override void CopyArgumentPackageCore(Arguments fromArgumentPackage, Arguments toArgumentPackage)
        {
            DescribedArgumentsHelper.Copy(fromArgumentPackage, toArgumentPackage);
        }

        #endregion

        /// <summary>
        /// ArgumentPackage class for SetColor.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            private BoreholeCollection wellCollection;
            private Borehole borehole;
            private bool setColor = true;
            private bool setSymbol = true;
            private Slb.Ocean.Petrel.DomainObject.Well.DictionaryBoreholeProperty filter;

            [Description("WellCollection", "description for new argument")]
            public BoreholeCollection WellCollection
            {
                internal get { return this.wellCollection; }
                set { this.wellCollection = value; }
            }

            [Description("ReferenceWell", "description for new argument")]
            public Borehole Borehole
            {
                internal get { return this.borehole; }
                set { this.borehole = value; }
            }

            [Description("SetColor", "description for new argument")]
            public bool SetColor
            {
                internal get { return this.setColor; }
                set { this.setColor = value; }
            }

            [Description("SetSymbol", "description for new argument")]
            public bool SetSymbol
            {
                internal get { return this.setSymbol; }
                set { this.setSymbol = value; }
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
        /// Gets the description of the SetColor
        /// </summary>
        public IDescription Description
        {
            get { return SetColorDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the SetColor.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class SetColorDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private  static SetColorDescription instance = new SetColorDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static SetColorDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of SetColor
            /// </summary>
            public string Name
            {
                get { return "Set well presentation"; }
            }
            /// <summary>
            /// Gets the short description of SetColor
            /// </summary>
            public string ShortDescription
            {
                get { return "Set well presentation using discrete filter property."; }
            }
            /// <summary>
            /// Gets the detailed description of SetColor
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