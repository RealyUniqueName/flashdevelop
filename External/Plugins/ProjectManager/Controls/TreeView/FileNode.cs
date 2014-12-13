using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ProjectManager.Helpers;
using ProjectManager.Projects;
using PluginCore.Utilities;

namespace ProjectManager.Controls.TreeView
{
    public delegate FileNode FileNodeFactory(string filePath);
    public delegate void FileNodeRefresh(FileNode node);

	/// <summary>
	/// Represents a file on disk.
	/// </summary>
	public class FileNode : GenericNode
	{
        static public readonly Dictionary<string, FileNodeFactory> FileAssociations 
            = new Dictionary<string, FileNodeFactory>();

        static public event FileNodeRefresh OnFileNodeRefresh;

		protected FileNode(string filePath) : base(filePath)
		{
			isDraggable = true;
			isRenamable = true;
		}

		/// <summary>
		/// Creates the correct type of FileNode based on the file name.
		/// </summary>
		public static FileNode Create(string filePath, Project project)
		{
            if (project != null) 
            {
                if (project.IsOutput(filePath))
                    return new ProjectOutputNode(filePath);
                if (project.IsInput(filePath))
                    return new InputSwfNode(filePath);
            }

            string ext = Path.GetExtension(filePath).ToLower();

            if (FileInspector.IsSwf(filePath, ext) || FileInspector.IsSwc(filePath, ext))
                return new SwfFileNode(filePath);
            else if (FileAssociations.ContainsKey(ext)) // custom nodes building
                return FileAssociations[ext](filePath);
            else
                return new FileNode(filePath);
		}

		public override void Refresh(bool recursive)
		{
			base.Refresh(recursive);

            string path = BackingPath;
            string ext = Path.GetExtension(path).ToLower();

            if (project.IsPathHidden(path))
                ImageIndex = Icons.HiddenFile.Index;
            else if ((FileInspector.IsActionScript(path, ext) || FileInspector.IsHaxeFile(path, ext)) && project.IsCompileTarget(path))
                ImageIndex = Icons.ActionScriptCompile.Index;
            else if (FileInspector.IsMxml(path, ext) && project.IsCompileTarget(path))
                ImageIndex = Icons.MxmlFileCompile.Index;
            else if (FileInspector.IsCss(path, ext) && project.IsCompileTarget(path))
                ImageIndex = Icons.ActionScriptCompile.Index;
            else if (FileInspector.IsSwc(path) && Parent == null) // external SWC library
                ImageIndex = Icons.Classpath.Index;
            else
                ImageIndex = Icons.GetImageForFile(path).Index;
			SelectedImageIndex = ImageIndex;

			Text = Path.GetFileName(path);

            if (project.IsLibraryAsset(path))
            {
                LibraryAsset asset = project.GetAsset(path);
				if (asset.IsSwc)
				{
					if (asset.SwfMode == SwfAssetMode.ExternalLibrary)
						ForeColorRequest = Color.DarkBlue;
					else if (asset.SwfMode == SwfAssetMode.Library)
						ForeColorRequest = Color.Blue;
					else if (asset.SwfMode == SwfAssetMode.IncludedLibrary)
						ForeColorRequest = Color.DarkCyan;
				} 
				else ForeColorRequest = Color.Blue;

                if (asset != null && asset.HasManualID)
                    Text += " (" + asset.ManualID + ")";
            }
            else
            {
                Color color = PluginCore.PluginBase.MainForm.GetThemeColor("ProjectTreeView.ForeColor");
                if (color != Color.Empty) ForeColorRequest = color;
                else ForeColorRequest = SystemColors.ControlText;
            }

            // hook for plugins
            if (OnFileNodeRefresh != null) OnFileNodeRefresh(this);
		}
	}

	/// <summary>
	/// A special FileNode that represents the project output file.  It won't disappear
	/// from the treeview while you're building.
	/// </summary>
	public class ProjectOutputNode : SwfFileNode
	{
		public ProjectOutputNode(string filePath) : base(filePath) {}

		public override void Refresh(bool recursive)
		{
			base.Refresh(recursive);

			if (!FileExists)
				ImageIndex = SelectedImageIndex = Icons.SwfFileHidden.Index;
		}
	}
}
