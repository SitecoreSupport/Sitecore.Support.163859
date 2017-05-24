using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentEditor.Pipelines.RenderContentEditor;

namespace Sitecore.Support.Shell.Applications.ContentEditor.Pipelines.RenderContentEditor
{
    public class RenderStandardContentEditor : Sitecore.Shell.Applications.ContentEditor.Pipelines.RenderContentEditor.RenderStandardContentEditor
    {
        /// <summary>
        /// Gets the field value.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <contract>
        ///   <requires name="args" condition="none" />
        /// </contract>
        public new void Process(RenderContentEditorArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            args.EditorFormatter = new EditorFormatter() { Arguments = args };
            base.Process(args);
        }
    }
}