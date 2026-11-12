using System;
using Grasshopper.Kernel;

namespace Masonry.GH
{
    public class BrickToolkitInfo : GH_AssemblyInfo
    {
        public override string Name => "BrickToolkit";   // TODO: final plugin name
        public override string Description => "Buildable parametric brickwork for Grasshopper.";
        public override Guid Id => new Guid("6f1d2a8e-3b4c-4f2e-9a71-5c0d8e2b7a10");
        public override string AuthorName => "iamilio";
        public override string AuthorContact => "";
    }
}
