<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="WebPartApprovalVolumesUserControl.ascx.cs" Inherits="Legenda.ProjSpace.Main.CONTROLTEMPLATES.Legenda.ProjSpace.Main.WebParts.WebPartApprovalVolumesUserControl" %>


<style type="text/css">

 	.custom-menu-container
    {
        background-color: #fff;
        border-bottom: 1px solid #e2e4e7;
        overflow-y: auto;
        max-height: 300px;
    }
    
    .ms-webpartPage-root 
    {
        border-spacing: 0px !important;
    }	
    
    
 
    table[id$="_ctl00_JsGridControl_leftpane_mainTable"] .jsgrid-header-content
    {
        background-color: #4169E1;
    }
    table[id$="_ctl00_JsGridControl_leftpane_mainTable"]  .jsgrid-header-core-content, table[id$="_ctl00_JsGridControl_rightpane_mainTable"] .jsgrid-header-core-content
    {
       white-space: normal;
       height: 52px !important;
       background-color: #4169E1;
       color: white !important;
       line-height: 23px !important;
    }

    table[id$="_ctl00_JsGridControl_leftpane_mainTable"]  .ms-positionRelative, table[id$="_ctl00_JsGridControl_rightpane_mainTable"]  .ms-positionRelative
    {
       height: 52px !important;
    }
 
    .jsgrid-header-core-content a
    {
       color: white !important;
    }
 
    table[id$="_ctl00_JsGridControl_leftpane_mainTable"] .jsgrid-header-expand, table[id$="_ctl00_JsGridControl_rightpane_mainTable"] .jsgrid-header-expand 
    {
       background-color: #4169E1 !important;
    }
 
    table[id$="_ctl00_JsGridControl_leftpane_mainTable"] .jsgrid-header-menu, table[id$="_ctl00_JsGridControl_rightpane_mainTable"] .jsgrid-header-menu 
    {
       background-color: #4169E1 !important;
    }
 
    .jsgrid-control-text
    {
       padding-left: 4px;
       padding-right: 4px;
    }
 
    
 
    table[id$="_ctl00_JsGridControl_leftpane_mainTable"] tbody tr td span.jsgrid-control-text
    {
        padding-right: 4px;
    }
    table[id$="_ctl00_JsGridControl_leftpane_mainTable"] tbody tr td
    {
        padding-left: 4px;
    }
 
    [id*="_ctl00_JsGridControl_leftpane_mainTable"]:first-child:first-child 
    {
       height: auto;
    }
 
    .jsgrid-header-eyebrow
    {
       height: 0px;
    }
 
    .scroll-bar-grip
    { 
        color:#444444;
    }
</style>


<script src="../../_layouts/15/Legenda.ProjSpace.Main/js/Jquery331.js" type="text/javascript"></script>
<script src="../../_layouts/15/Legenda.ProjSpace.Main/js/JsGridWritebackManagerApproval.js" type="text/javascript" asp-append-version="true"></script>
<div id="errorDiv" style="font-size: 18px; color: red;"></div><div></div>
<SharePoint:JSGrid ID="JsGridControl" runat="server" JSControllerClassName="WritebackGridManagerApproval" JSControllerInstanceName="WGMA" />


<script> 
    $(document).ready(function () {
        LoadSodByKey("sp.js", function () {
         
        });
        SP.SOD.registerSod('sp.datetimeutil.js', '/_layouts/15/sp.datetimeutil.js');
        SP.SOD.registerSod('jsgrid.gantt.js', '/_layouts/15/jsgrid.gantt.js');
        SP.SOD.loadMultiple(['sp.datetimeutil.js'], function () {
            SP.SOD.loadMultiple(['jsgrid.gantt.js'], function () {
                //Сюда надо прикрутить старт иициализации грида
            });
        });
    });
    
</script>

<script type="text/javascript">
    Type.registerNamespace("GridManager");

    GridManager = function () {

    };        
</script>
