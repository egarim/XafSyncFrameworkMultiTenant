using DevExpress.ExpressApp.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XafMT2.Module
{
    public interface ISyncProvider
    {


        public MySyncFrameworkDataSpaceProvider SyncFrameworkDataSpaceProvider { get; set; }
    }
}
