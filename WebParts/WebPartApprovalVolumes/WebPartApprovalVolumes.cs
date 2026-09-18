using System;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace Legenda.ProjSpace.Main.WebParts.WebPartApprovalVolumes
{
    [ToolboxItemAttribute(false)] // [ToolboxItem(false)]
    public class WebPartApprovalVolumes : WebPart
    {
        private const string _ascxPath = "~/_CONTROLTEMPLATES/15/Legenda.ProjSpace.Main.WebParts/WebPartApprovalVolumesUserControl.ascx";

        protected override void CreateChildControls()
        {
            try
            {
                Control child = Page.LoadControl(_ascxPath);
                Controls.Add(child);
            }
            catch(Exception ex)
            {
                var errLabel = new System.Web.UI.WebControls.Label();
                errLabel.ForeColor = System.Drawing.Color.Red;
                errLabel.Text = "Could not load WebPartApprovalVolumesUserControl " + ex.Message;
                Controls.Add(errLabel);
            }
        }
    }
}
