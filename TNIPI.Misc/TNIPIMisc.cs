using System;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;

namespace TNIPI.Misc
{
    /// <summary>
    /// This class will control the lifecycle of the Module.
    /// The order of the methods are the same as the calling order.
    /// </summary>
    [ModuleAppearance(typeof(TNIPIMiscAppearance))]
    public class TNIPIMisc : IModule
    {
        public TNIPIMisc()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        #region IModule Members

        /// <summary>
        /// This method runs once in the Module life; when it loaded into the petrel.
        /// This method called first.
        /// </summary>
        public void Initialize()
        {
            // TODO:  Add TNIPIMisc.Initialize implementation
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the not UI related components.
        /// (eg: datasource, plugin)
        /// </summary>
        public void Integrate()
        {
            // Registrations:

            // TODO:  Add TNIPIMisc.Integrate implementation

            BoreholeChange boreholechangeInstance = new BoreholeChange();
            //PetrelSystem.WorkflowEditor.Add(boreholechangeInstance);
            PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(boreholechangeInstance), "Plug-ins");

            SetColor setcolorInstance = new SetColor();
            //PetrelSystem.WorkflowEditor.Add(setcolorInstance);
            PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(setcolorInstance), "Plug-ins");

            CalcInterwellDistance calcinterwelldistanceInstance = new CalcInterwellDistance();
            //PetrelSystem.WorkflowEditor.Add(calcinterwelldistanceInstance);
            PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(calcinterwelldistanceInstance), "Plug-ins");

            CopyAttributes copyattributesInstance = new CopyAttributes();
            //PetrelSystem.WorkflowEditor.Add(copyattributesInstance);
            PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(copyattributesInstance), "Plug-ins");

            FindUserHistory finduserhistoryInstance = new FindUserHistory();
            //PetrelSystem.WorkflowEditor.Add(finduserhistoryInstance);
            PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(finduserhistoryInstance), "Plug-ins");

        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the UI related components.
        /// (eg: settingspages, treeextensions)
        /// </summary>
        public void IntegratePresentation()
        {
            // Registrations:


            // TODO:  Add TNIPIMisc.IntegratePresentation implementation
        }

        /// <summary>
        /// This method called once in the life of the module; 
        /// right before the module is unloaded. 
        /// It is usually when the application is closing.
        /// </summary>
        public void Disintegrate()
        {
            // TODO:  Add TNIPIMisc.Disintegrate implementation
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // TODO:  Add TNIPIMisc.Dispose implementation
        }

        #endregion

    }

    #region ModuleAppearance Class

    /// <summary>
    /// Appearance (or branding) for a Slb.Ocean.Core.IModule.
    /// This is associated with a module using Slb.Ocean.Core.ModuleAppearanceAttribute.
    /// </summary>
    internal class TNIPIMiscAppearance : IModuleAppearance
    {
        /// <summary>
        /// Description of the module.
        /// </summary>
        public string Description
        {
            get { return "Collection of miscellaneous plug-ins"; }
        }

        /// <summary>
        /// Display name for the module.
        /// </summary>
        public string DisplayName
        {
            get { return "Miscellaneous plug-ins"; }
        }

        /// <summary>
        /// Returns the name of a image resource.
        /// </summary>
        public string ImageResourceName
        {
            get { return null; }
        }

        /// <summary>
        /// A link to the publisher or null.
        /// </summary>
        public Uri ModuleUri
        {
            get { return null; }
        }
    }

    #endregion
}