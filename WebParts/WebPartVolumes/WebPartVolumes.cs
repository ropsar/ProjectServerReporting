using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace Legenda.ProjSpace.Main.WebParts.WebPartVolumes
{
    [ToolboxItemAttribute(false)]
    public class WebPartVolumes : WebPart
    {
        private const string _ascxPath = "~/_CONTROLTEMPLATES/15/Legenda.ProjSpace.Main.WebParts/WebPartVolumesUserControl.ascx";

        protected override void CreateChildControls()
        {
            try
            {
                Control child = Page.LoadControl(_ascxPath);
                Controls.Add(child);
            }
            catch (Exception ex)
            {
                var errLabel = new System.Web.UI.WebControls.Label();
                errLabel.ForeColor = System.Drawing.Color.Red;
                errLabel.Text = "Could not load WebPartVolumesUserControl " + ex.Message;
                Controls.Add(errLabel);
            }
        }
    }
}
