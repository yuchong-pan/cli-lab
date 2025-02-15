﻿using System;
using System.Collections.Generic;
using System.CommandLine.Rendering.Views;
using Microsoft.DotNet.Tools.Uninstall.Shared.BundleInfo;
using Microsoft.DotNet.Tools.Uninstall.Shared.BundleInfo.Versioning;

namespace Microsoft.DotNet.Tools.Uninstall.Shared.Configs
{
    internal abstract class BundleTypePrintInfo
    {
        public abstract BundleType Type { get; }

        public string Header { get; }
        public Func<IList<Bundle>, GridView> GridViewGenerator { get; }
        public string OptionName { get; }

        protected BundleTypePrintInfo(string header, Func<IList<Bundle>, GridView> gridViewGenerator, string optionName)
        {
            Header = header ?? throw new ArgumentNullException();
            GridViewGenerator = gridViewGenerator ?? throw new ArgumentNullException();
            OptionName = optionName ?? throw new ArgumentNullException();
        }

        public abstract IEnumerable<Bundle> Filter(IEnumerable<Bundle> bundles);
    }

    internal class BundleTypePrintInfo<TBundleVersion> : BundleTypePrintInfo
        where TBundleVersion : BundleVersion, IComparable<TBundleVersion>, new()
    {
        public override BundleType Type => new TBundleVersion().Type;

        public BundleTypePrintInfo(string header, Func<IList<Bundle>, GridView> gridViewGenerator, string optionName) :
            base(header, gridViewGenerator, optionName)
        { }

        public override IEnumerable<Bundle> Filter(IEnumerable<Bundle> bundles)
        {
            return Bundle<TBundleVersion>.FilterWithSameBundleType(bundles);
        }
    }
}
