using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotoshopFile;

namespace OpapPsdSplitter
{
    public static class LayerExtender
    {
        public static LayerSectionType GetSectionType(this Layer layer)
        {

            LayerSectionInfo sec = layer.AdditionalInfo
                .OfType<LayerSectionInfo>().FirstOrDefault();

            LayerSectionType secType = LayerSectionType.Layer;
            if (sec != null)
            {
                secType = sec.SectionType;
            }

            return secType;
        }
    }
}
