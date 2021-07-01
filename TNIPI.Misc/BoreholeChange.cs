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
    /// This class contains all the methods and subclasses of the BoreholeChange.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    public class BoreholeChange : Workstep<BoreholeChange.Arguments>, IPresentation, IDescriptionSource
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
                Slb.Ocean.Petrel.DomainObject.Well.Surface ifc = argumentPackage.Horizon;
                Slb.Ocean.Petrel.DomainObject.Shapes.Surface sfc = argumentPackage.Surface;
                double zflat = argumentPackage.Zflat;
                DictionaryBoreholeProperty filter = argumentPackage.Filter;

                if (bhColl == null)
                {
                    MessageBox.Show("Well collection can't be null", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (sfc == null && zflat > 0.0d)
                {
                    if (MessageBox.Show("Zflat is positive. Do you want to continue?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        return;
                }

                foreach (Borehole bh in bhList)
                {
                    if (filter != null)
                    {
                        int flt = bh.PropertyAccess.GetPropertyValue<int>(filter);
                        if (flt == 0 || flt == Marker.UndefinedDictionaryPropertyValue)
                            continue;
                    }

                    if (ifc.GetMarkerCount(bh) == 0)
                        continue;

                    double minmd = double.MaxValue;
                    Marker minmarker = null;
                    foreach (Marker marker in ifc.GetMarkers(bh))
                    {
                        if (marker.MD < minmd)
                        {
                            minmd = marker.MD;
                            minmarker = marker;
                        }
                    }

                    Point3 bhpnt;
                    if (sfc != null)
                    {
                        bhpnt = bh.Transform3(Domain.MD, minmd, sfc.Domain);
                        Point3 minpnt = new Point3(bhpnt.X, bhpnt.Y, -1e10); //float.MinValue);
                        Point3 maxpnt = new Point3(bhpnt.X, bhpnt.Y, +1e10); //float.MaxValue);
                        Polyline3 pl = new Polyline3(new Point3[2] { minpnt, maxpnt });

                        Point3 intspnt = null;
                        try
                        {
                            ISurfaceIntersectionService isis = CoreSystem.GetService<ISurfaceIntersectionService>(sfc);
                            foreach (PolylineSurfaceIntersection plsi in isis.GetSurfacePolyLineIntersection(sfc, pl))
                                intspnt = plsi.IntersectionPoint;
                        }
                        catch (Exception exc)
                        {
                            PetrelLogger.InfoOutputWindow(bh.Name + ": " + exc.Message);
                            continue;
                        }

                        if (intspnt == null)
                        {
                            PetrelLogger.InfoOutputWindow(bh.Name + ": intersection not found");
                            continue;
                        }

                        zflat = intspnt.Z;
                    }
                    else
                        bhpnt = bh.Transform3(Domain.MD, minmd, Domain.ELEVATION_DEPTH);

                    double shift = zflat - bhpnt.Z;
                    using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
                    {
                        trans.Lock(bh);

                        try
                        {
                            if (sfc != null)
                            {
                                Point3 kbpnt = bh.Transform3(Domain.ELEVATION_DEPTH, bh.KellyBushing, sfc.Domain);
                                double newz = kbpnt.Z + shift;
                                Point3 newkbpnt = bh.Transform3(sfc.Domain, newz, Domain.ELEVATION_DEPTH);
                                bh.KellyBushing = newkbpnt.Z;
                            }
                            else
                                bh.KellyBushing = bh.KellyBushing + shift;
                        }
                        catch (Exception exc)
                        {
                            PetrelLogger.InfoOutputWindow(bh.Name + ": " + exc.Message);
                        }

                        trans.Commit();
                    }

                    PetrelLogger.InfoOutputWindow(bh.Name + ": KB shift = " + shift.ToString("F02"));
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
        /// ArgumentPackage class for BoreholeChange.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            private Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection wellCollection;
            private Slb.Ocean.Petrel.DomainObject.Well.Surface horizon;
            private Slb.Ocean.Petrel.DomainObject.Shapes.Surface surface;
            private double zflat = 0.0d;
            private Slb.Ocean.Petrel.DomainObject.Well.DictionaryBoreholeProperty filter;

            [Description("WellCollection", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection WellCollection
            {
                internal get { return this.wellCollection; }
                set { this.wellCollection = value; }
            }

            [Description("Horizon", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.Surface Horizon
            {
                internal get { return this.horizon; }
                set { this.horizon = value; }
            }

            [Description("Surface", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Shapes.Surface Surface
            {
                internal get { return this.surface; }
                set { this.surface = value; }
            }

            [Description("Plane Z", "description for new argument")]
            public double Zflat
            {
                internal get { return this.zflat; }
                set { this.zflat = value; }
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
        /// Gets the description of the BoreholeChange
        /// </summary>
        public IDescription Description
        {
            get { return BoreholeChangeDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the BoreholeChange.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class BoreholeChangeDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private  static BoreholeChangeDescription instance = new BoreholeChangeDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static BoreholeChangeDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of BoreholeChange
            /// </summary>
            public string Name
            {
                get { return "Shift well KB"; }
            }
            /// <summary>
            /// Gets the short description of BoreholeChange
            /// </summary>
            public string ShortDescription
            {
                get { return "Change well KBs so that well tops will coincide with a given surface/plane using discrete filter property."; }
            }
            /// <summary>
            /// Gets the detailed description of BoreholeChange
            /// </summary>
            public string Description
            {
                get { return "If surface is not set, plane Z value (TVDSS) is used. If filter is set, only wells with defined non-zero filter value are considered."; }
            }

            #endregion
        }
        #endregion


    }
}