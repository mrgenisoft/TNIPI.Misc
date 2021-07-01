using System;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;

using Slb.Ocean.Core;
using Slb.Ocean.Geometry;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Well;
using Slb.Ocean.Petrel.DomainObject.Shapes;

namespace TNIPI.Misc
{
    /// <summary>
    /// This class contains all the methods and subclasses of the CalcInterwellDistance.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    public class CalcInterwellDistance : Workstep<CalcInterwellDistance.Arguments>, IPresentation, IDescriptionSource
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
                double thrsh = argumentPackage.Threshold;

                if (bhColl == null)
                {
                    MessageBox.Show("Well collection can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (thrsh <= 0.0)
                {
                    MessageBox.Show("Threshold must be positive", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<Borehole> bhList1 = new List<Borehole>();
                List<Borehole> bhList2 = new List<Borehole>();

                FillBoreholeList(bhColl, bhList1);
                FillBoreholeList(bhColl, bhList2);

                PetrelLogger.InfoOutputWindow("Well 1\tWell 2\tDistance");
                foreach (Borehole bh1 in bhList1)
                {
                    foreach (Borehole bh2 in bhList2)
                    {
                        if (bh1.Equals(bh2))
                            continue;

                        double botmd1 = 0.0d;
                        foreach (TrajectoryRecord tr in bh1.Trajectory.Records)
                            if (botmd1 < tr.MD)
                                botmd1 = tr.MD;

                        double botmd2 = 0.0d;
                        foreach (TrajectoryRecord tr in bh2.Trajectory.Records)
                            if (botmd2 < tr.MD)
                                botmd2 = tr.MD;

                        Point3 pnt1 = bh1.Transform3(Domain.MD, botmd1, Domain.ELEVATION_DEPTH);
                        Point3 pnt2 = bh2.Transform3(Domain.MD, botmd2, Domain.ELEVATION_DEPTH);
                        double dist = Math.Sqrt(Math.Pow(pnt1.X - pnt2.X, 2) + Math.Pow(pnt1.Y - pnt2.Y, 2));

                        if (dist < thrsh)
                            PetrelLogger.InfoOutputWindow(bh1.Name + "\t" + bh2.Name + "\t" + dist.ToString("F01"));
                    }

                    bhList2.Remove(bh1);
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
        /// ArgumentPackage class for CalcInterwellDistance.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            private Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection wellCollection;
            private double threshold = 50;

            [Description("WellCollection", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection WellCollection
            {
                internal get { return this.wellCollection; }
                set { this.wellCollection = value; }
            }

            [Description("Threshold", "description for new argument")]
            public double Threshold
            {
                internal get { return this.threshold; }
                set { this.threshold = value; }
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
        /// Gets the description of the CalcInterwellDistance
        /// </summary>
        public IDescription Description
        {
            get { return CalcInterwellDistanceDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the CalcInterwellDistance.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class CalcInterwellDistanceDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private static CalcInterwellDistanceDescription instance = new CalcInterwellDistanceDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static CalcInterwellDistanceDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of CalcInterwellDistance
            /// </summary>
            public string Name
            {
                get { return "Find close wells"; }
            }
            /// <summary>
            /// Gets the short description of CalcInterwellDistance
            /// </summary>
            public string ShortDescription
            {
                get { return "Find close wells by calculating distance between borehole bottoms and applying threshold."; }
            }
            /// <summary>
            /// Gets the detailed description of CalcInterwellDistance
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