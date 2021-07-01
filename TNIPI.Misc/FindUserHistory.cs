using System;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;

using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Well;

namespace TNIPI.Misc
{
    /// <summary>
    /// This class contains all the methods and subclasses of the FindUserHistory.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    public class FindUserHistory : Workstep<FindUserHistory.Arguments>, IPresentation, IDescriptionSource
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
                string user = argumentPackage.User;
                string action = argumentPackage.Action;

                if (bhColl == null)
                {
                    MessageBox.Show("Well collection can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<Borehole> bhList = new List<Borehole>();
                FillBoreholeList(bhColl, bhList);

                if (bhList.Count == 0)
                {
                    MessageBox.Show("Wells not found", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                foreach (Borehole bh in bhList)
                {
                    bool found = false;
                    DateTime maxdate = DateTime.MinValue;
                    string args = string.Empty;
                    foreach (HistoryEntry he in HistoryService.GetHistory(bh))
                    {
                        if(he.UserName.Equals(user) && he.Operation.Equals(action))
                        {
                            found = true;
                            if (he.BeginDate > maxdate)
                            {
                                maxdate = he.BeginDate;
                                args = he.Arguments;
                            }
                        }
                    }

                    if (found)
                        PetrelLogger.InfoOutputWindow(bh.Name + " " + maxdate.ToString() + " " + args);
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
        /// ArgumentPackage class for FindUserHistory.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            private Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection wellCollection;
            private string user;
            private string action;

            [Description("WellCollection", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection WellCollection
            {
                internal get { return this.wellCollection; }
                set { this.wellCollection = value; }
            }

            [Description("User", "description for new argument")]
            public string User
            {
                internal get { return this.user; }
                set { this.user = value; }
            }

            [Description("Action", "description for new argument")]
            public string Action
            {
                internal get { return this.action; }
                set { this.action = value; }
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
        /// Gets the description of the FindUserHistory
        /// </summary>
        public IDescription Description
        {
            get { return FindUserHistoryDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the FindUserHistory.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class FindUserHistoryDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private  static FindUserHistoryDescription instance = new FindUserHistoryDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static FindUserHistoryDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of FindUserHistory
            /// </summary>
            public string Name
            {
                get { return "Find user history"; }
            }
            /// <summary>
            /// Gets the short description of FindUserHistory
            /// </summary>
            public string ShortDescription
            {
                get { return ""; }
            }
            /// <summary>
            /// Gets the detailed description of FindUserHistory
            /// </summary>
            public string Description
            {
                get { return ""; }
            }

            #endregion
        }

        #endregion
    }
}