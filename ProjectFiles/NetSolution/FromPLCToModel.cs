#region Using directives

using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Linq;
using UAManagedCore;
using utilx.Utils;

#endregion

public class FromPLCToModel : BaseNetLogic
{
    [ExportMethod]
    public void GenerateNodesIntoModel()
    {
        generateNodesTask = new LongRunningTask(GenerateNodesMethod, LogicObject);
        generateNodesTask.Start();
    }

    private void GenerateNodesMethod()
    {
        var startingNode = InformationModel.Get(LogicObject.GetVariable("StartingNodeToFetch").Value);
        UtilsTags utilsTags = new UtilsTags(LogicObject, startingNode);
        utilsTags.GenerateNodesIntoModel();
        generateNodesTask?.Dispose();
    }

    private LongRunningTask generateNodesTask;
}

namespace utilx.Utils
{
    internal class UtilsTags
    {
        private readonly IUAObject _logicObject;
        private readonly IUANode _startingNode;
        private readonly bool _deleteBrokenModelTags;
        private readonly bool _deleteExistingTags;
        private Folder _modelFolder;

        public UtilsTags(IUAObject logicObject, IUANode _startingNode)
        {
            _logicObject = logicObject;
            _deleteExistingTags = logicObject.GetVariable("DeleteExistingTags").Value;
            this._startingNode = _startingNode;
        }

        /// <summary>
        /// Generatets a set of objects and variables in model in order to have a "copy" of a set of imported tags, retrieved from a starting node
        /// </summary>
        public void GenerateNodesIntoModel()
        {
            _modelFolder = InformationModel.Get<Folder>(_logicObject.GetVariable("TargetFolder").Value);
            if (_modelFolder == null)
            {
                Log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name, "Cannot get to target folder");
                return;
            }
            CreateModelTag(_startingNode, _modelFolder, firstLoop: true);
            CheckDatabinds();
        }

        #region private methods


        private void CreateModelTag(IUANode fieldNode, IUANode parentNode, string browseNamePrefix = "", bool firstLoop = false)
        {
            switch (fieldNode)
            {
                case TagStructure _:
                    if (!IsTagStructureArray(fieldNode))
                    {
                        CreateOrUpdateObject(fieldNode, parentNode, browseNamePrefix);
                    }
                    else
                    {
                        CreateOrUpdateObjectArray(fieldNode, parentNode, browseNamePrefix);
                    }
                    break;
                case FTOptix.Core.Folder:
                    IUANode newFolder = null;
                    if (!firstLoop)
                    {
                        newFolder = CreateFolder(fieldNode, parentNode, browseNamePrefix);
                    }
                    else
                    {
                        newFolder = parentNode;
                    }

                    foreach (var children in fieldNode.Children)
                    {
                        CreateModelTag(children, newFolder, browseNamePrefix);
                    }
                    break;
                default:
                    CreateOrUpdateVariable(fieldNode, parentNode, browseNamePrefix);
                    break;
            }
        }

        private static bool IsTagStructureArray(IUANode fieldNode) => ((TagStructure)fieldNode).ArrayDimensions.Length != 0;

        private IUANode CreateFolder(IUANode fieldNode, IUANode parentNode, string browseNamePrefix = "")
        {
            if (parentNode.Get<FTOptix.Core.Folder>(fieldNode.BrowseName) == null)
            {
                var newFolder = InformationModel.Make<FTOptix.Core.Folder>(fieldNode.BrowseName);
                parentNode.Add(newFolder);
                Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name, $"Creating \"{Log.Node(newFolder)}\"");
                return (IUANode)newFolder;
            }
            else
            {
                if (_deleteExistingTags)
                {
                    Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name, $"Deleting \"{Log.Node(fieldNode)}\" (DeleteExistingTags is set to True)");
                    parentNode.Get<FTOptix.Core.Folder>(fieldNode.BrowseName).Children.Clear();
                }
                else
                {
                    Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name, $"\"{Log.Node(fieldNode)}\" already exist, skipping creation or children deletion (DeleteExistingTags is set to False)");
                }
                return (IUANode)parentNode.Get<FTOptix.Core.Folder>(fieldNode.BrowseName);
            }
        }

        private void CreateOrUpdateObjectArray(IUANode fieldNode, IUANode parentNode, string browseNamePrefix = "")
        {
            var tagStructureArrayTemp = (TagStructure)fieldNode;
            foreach (var c in tagStructureArrayTemp.Children.Where(c => !IsArrayDimentionsVar(c)))
            {
                CreateModelTag(c, parentNode, fieldNode.BrowseName + "_");
            }
        }

        private void CreateOrUpdateObject(IUANode fieldNode, IUANode parentNode, string browseNamePrefix = "")
        {
            var existingNode = GetChild(fieldNode, parentNode, browseNamePrefix);
            // Replacing "/" with "_". Nodes with browsename "/" are not allowed
            var filedNodeBrowseName = fieldNode.BrowseName.Replace("/", "_");

            if (existingNode == null)
            {
                existingNode = InformationModel.MakeObject(browseNamePrefix + filedNodeBrowseName);
                parentNode.Add(existingNode);
                Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name, $"Creating \"{Log.Node(existingNode)}\" object");
            }
            else
            {
                Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name, $"Updating \"{Log.Node(existingNode)}\" object");
            }

            foreach (var t in fieldNode.Children.Where(c => !IsArrayDimentionsVar(c)))
            {
                CreateModelTag(t, existingNode);
            }
        }

        private void CreateOrUpdateVariable(IUANode fieldNode, IUANode parentNode, string browseNamePrefix = "")
        {
            if (IsArrayDimentionsVar(fieldNode)) return;
            var existingNode = GetChild(fieldNode, parentNode, browseNamePrefix);

            if (existingNode == null)
            {
                var mTag = (IUAVariable)fieldNode;
                // Replacing "/" with "_". Nodes with browsename "/" are not allowed
                var tagBrowseName = mTag.BrowseName.Replace("/", "_");
                existingNode = InformationModel.MakeVariable(tagBrowseName, mTag.DataType, mTag.ArrayDimensions);
                parentNode.Add(existingNode);
                Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name, $"Creating \"{Log.Node(existingNode)}\" variable");
            }
            else
            {
                Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name, $"Updating \"{Log.Node(existingNode)}\" object");
            }
            ((IUAVariable)existingNode).SetDynamicLink((UAVariable)fieldNode, FTOptix.CoreBase.DynamicLinkMode.ReadWrite);
        }

        private bool IsArrayDimentionsVar(IUANode n) => n.BrowseName.ToLower().Contains("arraydimen");

        private IUANode GetChild(IUANode child, IUANode parent, string browseNamePrefix = "") => parent.Children.FirstOrDefault(c => c.BrowseName == browseNamePrefix + child.BrowseName);

        private void CheckDatabinds()
        {
            var lDataBinds = _modelFolder.FindNodesByType<IUAVariable>().Where<IUAVariable>(v => { return v.BrowseName == "DynamicLink"; });
            foreach (var vDataBind in lDataBinds)
            {
                var IsResolved = _logicObject.Context.ResolvePath(vDataBind.Owner, vDataBind.Value).ResolvedNode;
                if (IsResolved == null)
                {
                    Log.Warning(System.Reflection.MethodBase.GetCurrentMethod().Name, $"\"{Log.Node(vDataBind.Owner)}\" has unresolved databind, you may need to either: manually reimport the missing PLC tag(s), manually delete the unresolved Model variable(s) or set DeleteExistingTags to True (which may lead to unresolved DynamicLinks somewhere else)");
                }
            }
        }

        #endregion private methods
    }
}