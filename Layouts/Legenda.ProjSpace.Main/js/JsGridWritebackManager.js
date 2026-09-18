var _representation = [];
var _projects = [];
var _filterTasksOut = [];
var _filterTasks = [];
var _kindWorks = [];
var _floor = [];
var _blok = [];
var _blok2 = [];
var _capture = [];
var _captureOut = [];
var _kindWorksOut = [];
var _floorOut = [];
var _bloksOut = [];
var _bloks2Out = [];
var _targetBasePlan = null;
var _basePlans = [];
var _jsGridControl;

Type.registerNamespace("WritebackGridManager");
WritebackGridManager = function () {
    var CMD_GetView = "GetView";
    var CMD_GetByFilters = "GetByFilters";
    var CMD_Submitted_Approval = 'SubmittedApproval';
    var CMD_SaveView = "SaveView";
    var globalNotificationID = "";
    var _GridPrefix;
    var _props;
    var _jsGridParams;
    var _gridData;
    var _dataSource;
    var _changesToSave = {};
    var _propertiesFilter = {};
    var _banner;
    var bannerString = "Загрузка...";
    var _submittedApproval = {};
    var _errorNotifiId = "errorNotifiId";
    var _targetViewName = "";
    var _targetTimeScale = 0;
    var _schedulePeriod = 4;
    var _periodStart = "";
    var _periodFinish = "";
    var _selectedProjUid = null;
    var _gridColumn = null;
    var _newViewName = "";
    var _sumTasksGuids = [];
    var _orderByColumnName;
    var _isDescending;
    var _ganttZoomLevel = 3;
    var lock = 0;
    var _dateStartOld = null;
    var _dateFinishOld = null;

    this.Filter = function () {
        _jsGridParams.tableViewParams.rowViewParams.topViewIdx = 0;
        currentTopRowIdx = 0;
        _propertiesFilter["Layout"] = 0;
        var diffTracker = _jsGridControl.GetDiffTracker();
        if (!IsChangesVolume(diffTracker)) {
            BindJSGrid(CMD_GetByFilters);
        }
        else
            alert("Есть несохраненные данные. Сохраните данные. Только после этого можно будет применить фильтрацию");
    }

    function IsChangesVolume(obj) {
        if (obj.AnyChanges()) {
            var uniquePropertyChanges = obj.GetUniquePropertyChanges();
            if (IsNotNull(uniquePropertyChanges)) {
                for (key in uniquePropertyChanges) {
                    var item = uniquePropertyChanges[key];
                    for (key in item) {
                        if (key != "IsCommentDate") { return true; }
                    }
                }
            }
        }
        else { return false; }
        return false;
    }



    this.Init = function (jsGridControl, initialData, props) {
        _jsGridControl = jsGridControl;
        _GridPrefix = _jsGridControl.parentNode.id.replace("JsGridControl", "");
        _banner = ban = new SP.LoadingBanner(_jsGridControl.parentNode, _jsGridControl.parentNode.id + '_disable_banner_second');
        _props = props;

        RegisterCustomDateTimePropType();
        CreatePsGridFieldsToolTipWidget();
        CreatePsGridFieldsToolTipDisplay();

        ///Create CustomType
        this._PsGridFieldsToolTip = CreatePsGridFieldsToolTip('String');
        SP.JsGrid.PropertyType.RegisterNewCustomPropType(this._PsGridFieldsToolTip, this._PsGridFieldsToolTip.DisplayControlNames, SP.JsGrid.EditControl.Type.EditBox, this._PsGridFieldsToolTip.widgetControlNames);

        // Delegate to handle sort
        jsGridControl.SetDelegate(SP.JsGrid.DelegateType.Sort, function (newSortedCols) {
            _orderByColumnName = newSortedCols[0].columnName;
            _isDescending = newSortedCols[0].isDescending;

            BindJSGrid(CMD_GetByFilters);
        });
        jsGridControl.AttachEvent(SP.JsGrid.EventType.OnPropertyChanged, function (args) {

            var r = _jsGridControl.GetRecord(args.recordKey);
            if (_jsGridControl.AnyErrorsInRecord(args.recordKey)) {
                _jsGridControl.ClearAllErrorsOnRow(args.recordKey);
            }


            if (!_changesToSave[args.recordKey])
                _changesToSave[args.recordKey] = {};
            _changesToSave[args.recordKey][args.fieldKey] = args.newProp.localized;
            jsGridControl.RefreshRow(args.recordKey);
        });

        _propertiesFilter["Layout"] = 2;

        BindJSGrid(CMD_GetByFilters);

        window.onbeforeunload = function () {

            try {
                var diffTracker = _jsGridControl.GetDiffTracker();
                if (IsChangesVolume(diffTracker)) {
                    return "Изменения не были сохранены. Если вы продолжите, изменения будут потеряны.";
                    //return "Changes on this page haven't been saved. If you continue, the changes will be lost.";
                }
            }
            catch (e) {
                console.log(e);
            }
        }
    }


    function IsNotNull(obj) {
        return typeof (obj) != 'undefined' && obj != null ? true : false
    }

    function BindJSGrid(command) {

        var periodStartStr = "";
        var periodFinishStr = "";
        if (_periodStart != null && _periodStart != undefined && _periodStart != "" && _periodFinish != null && _periodFinish != undefined && _periodFinish != "") {
            _periodStart = new Date(_periodStart);
            periodStartStr = _periodStart.getFullYear() + "." + (_periodStart.getMonth() + 1) + "." + _periodStart.getDate();
            _periodFinish = new Date(_periodFinish);
            periodFinishStr = _periodFinish.getFullYear() + "." + (_periodFinish.getMonth() + 1) + "." + _periodFinish.getDate();
        }
        _targetBasePlan = _targetBasePlan == null ? null : _targetBasePlan * 1;
        var args = Sys.Serialization.JavaScriptSerializer.serialize({
            Command: command,
            SubmittedApproval: _submittedApproval,
            Properties: _propertiesFilter,
            ViewName: command == "SaveView" ? _newViewName : _targetViewName,
            TypeView: _targetTimeScale,
            Period: _schedulePeriod,
            PeriodStart: _periodStart,
            PeriodFinish: _periodFinish,
            ProjUid: (_selectedProjUid == null ? "" : _selectedProjUid),
            TaskUids: _filterTasksOut,
            KindWork: _kindWorksOut,
            Bloks: _bloksOut,
            Bloks2: _bloks2Out,
            Capture: _captureOut,
            Stages: _floorOut,
            BasePlan: _targetBasePlan,
            GridColumn: _gridColumn,
            OrderByColumnName: _orderByColumnName,
            IsDescending: _isDescending,
            PeriodStartStr: periodStartStr,
            PeriodFinishStr: periodFinishStr
        });

        _banner.Show(bannerString);
        if (typeof (_props) !== 'undefined' && null !== _props) {
            eval(_props.callbackScript);
        }
    }

    function InitializeJSGridColumnsSettings() {
        var colArray = _jsGridControl._GetPaneManager().GetColumns();
        var jsGridColumnSettings = []

        for (var i = 0; i < colArray.length; i++) {
            var column = colArray[i];
            var columnKey = column.columnKey;
            jsGridColumnSettings.push({
                Name: column.columnKey,
                Width: column.width,
                IsVisible: column.isVisible

            });
        }

        return jsGridColumnSettings;
    }

    this.DisplayProjectsData = function (data) {
        _banner.Hide();
        if (SP.UI.Status)
            SP.UI.Status.removeStatus(_errorNotifiId);

        // Clear the grid.
        if (_jsGridControl.IsInitialized()) {
            _jsGridControl.ClearChanges();
            _jsGridControl.ClearTableView();
        }

        // Show data in the grid.
        if (data && data !== '') {
            var responseData = Sys.Serialization.JavaScriptSerializer.deserialize(data);
            var isReloadView = false;
            var isReloadProject = false;
            var isReloadTask = false;
            _kindWorks = [];
            _kindWorksOut = [];
            _blok = [];
            _bloksOut = [];
            _blok2 = [];
            _bloks2Out = [];
            _capture = [];
            _captureOut = [];
            _floor = [];
            _floorOut = [];
            _filterTasks = [];
            _basePlans = [];
            _startPeriod = responseData.startPeriod;
            _reportMonth = responseData.reportMonth;
            _representation = responseData.Views;
            _projects = responseData.Projects.sort((a, b) => a.Name > b.Name ? 1 : -1);
            _targetBasePlan = responseData.BasePlan;
            if (responseData.BasePlans != undefined)
                _basePlans = responseData.BasePlans;
            if (responseData.KindWork != undefined)
                _kindWorks = responseData.KindWork;//.sort((a, b) => a.localeCompare(b));
            if (responseData.CurrentKindWork != undefined)
                _kindWorksOut = responseData.CurrentKindWork.sort((a, b) => a.localeCompare(b));
            if (responseData.Bloks != undefined)
                _blok = responseData.Bloks.sort((a, b) => a.localeCompare(b));
            if (responseData.CurrentBloks != undefined)
                _bloksOut = responseData.CurrentBloks.sort((a, b) => a.localeCompare(b));
            if (responseData.Bloks2 != undefined)
                _blok2 = responseData.Bloks2.sort((a, b) => a.localeCompare(b));
            if (responseData.Capture != undefined)
                _capture = responseData.Capture.sort((a, b) => a.localeCompare(b));
            if (responseData.CurrentCapture != undefined)
                _captureOut = responseData.CurrentCapture.sort((a, b) => a.localeCompare(b));
            if (responseData.CurrentBloks2 != undefined)
                _bloks2Out = responseData.CurrentBloks2.sort((a, b) => a.localeCompare(b));
            if (responseData.Stages != undefined)
                _floor = responseData.Stages.sort((a, b) => a.localeCompare(b));
            if (responseData.CurrentStages != undefined)
                _floorOut = responseData.CurrentStages.sort((a, b) => a.localeCompare(b));
            if (responseData.SumTaskNames != undefined) {
                for (key in responseData.SumTaskNames) {
                    var task = responseData.SumTaskNames[key];
                    if (task.IsSummary)
                        _sumTasksGuids.push(task.Uid);
                    else
                        _filterTasks.push({ Name: task.Name, Id: task.Uid });

                }
                _filterTasks = _filterTasks.sort((a, b) => a.Name > b.Name ? 1 : -1);
            }


            for (var i = 0; i < responseData.TaskUids.length; i++) {
                var item = responseData.TaskUids[i];
                if (_filterTasksOut.indexOf(item) == -1) {
                    isReloadTask = true;
                }
            }


            if (isReloadTask) {
                _filterTasksOut = responseData.TaskUids;
            }
            if (responseData.ProjUid != _selectedProjUid || responseData.ViewName != _targetViewName) {
                if (responseData.ProjUid != _selectedProjUid) {
                    _selectedProjUid = responseData.ProjUid;
                    isReloadProject = true;
                }
                if (responseData.ViewName != _targetViewName) {
                    _targetViewName = responseData.ViewName;
                    isReloadView = true;
                }



            }
            if (responseData.TypeView != undefined)
                _targetTimeScale = responseData.TypeView;
            if (responseData.Period != undefined)
                _schedulePeriod = responseData.Period;
            if (responseData.PeriodStart != undefined)
                _periodStart = responseData.PeriodStart;
            if (responseData.PeriodFinish != undefined)
                _periodFinish = responseData.PeriodFinish;



            //Загрузка рибона
            if ($('.ms-cui-tabContainer')[0] == undefined)
                SelectRibbonTab("Ribbon.WebPartPage", true);


            var tab = $('.ms-cui-tabContainer')[0].children[0];
            if (tab != null) {
                if ($(tab)[0].id != 'TaskReport.Id') {
                    ExecuteOrDelayUntilScriptLoaded(function () {
                        var pm = SP.Ribbon.PageManager.get_instance();


                        pm.add_ribbonInited(function () {

                            createTab();
                        });

                        var ribbon = null;
                        try {
                            ribbon = pm.get_ribbon();
                        }
                        catch (e) { }

                        if (!ribbon) {
                            if (typeof (_ribbonStartInit) == "function")
                                _ribbonStartInit(_ribbon.initialTabId, false, null);
                        }
                        else {
                            createTab();
                        }
                    }, "sp.ribbon.js");
                }
                else if (isReloadProject || isReloadView || isReloadTask)
                    ClearSelectionAndLoadNew();
            }

            var responsegridS = responseData.gridS;
            if (!IsNotNull(responseData.gridS)) { responsegridS = responseData.gridJson; }
            _gridData = SP.JsGrid.Deserializer.DeserializeFromJson(responsegridS);

            if (!_dataSource)
                _dataSource = new SP.JsGrid.StaticDataSource(_gridData);
            else {
                //console.log(_gridData);
                _dataSource.LoadSerializedData(_gridData);
            }



            _jsGridParams = _dataSource.InitJsGridParams();

            _jsGridParams.tableViewParams.bRecordIndicatorCheckboxesEnabled = true;
            _jsGridParams.tableViewParams.checkSelectionCheckboxHiddenRecordKeys = _sumTasksGuids;
            _jsGridParams.bEnableDiffTracking = true;
            _jsGridParams.tableViewParams.bEditingEnabled = true;

            _jsGridParams.styleManager.RegisterCellStyle('TextRightAlign', SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { textAlign: 'right' }));
            _jsGridParams.styleManager.RegisterCellStyle("IsTask", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#87CEFA", fontWeight: '700' }));
            _jsGridParams.styleManager.RegisterCellStyle("IsNewTask", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#fff6c1", fontWeight: 'bold' }));
            _jsGridParams.styleManager.RegisterCellStyle("VolumeIsAgree", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#90EE90" }));
            _jsGridParams.styleManager.RegisterCellStyle("VolumeIsNotAgree", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#FA8072" }));
            _jsGridParams.styleManager.RegisterCellStyle("VolumeSend", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#fff6c1" }));
            _jsGridParams.styleManager.RegisterCellStyle("VolumeChange", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#C0C0C0" }));
            _jsGridParams.styleManager.RegisterCellStyle("BlockingUnitPrice", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#87CEFA" }));
            _jsGridParams.styleManager.RegisterCellStyle("BlockingPeriods", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#8FBC8F" }));
            _jsGridParams.styleManager.RegisterCellStyle("BlockingVolumeIsAgree", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#74a374" }));
            _jsGridParams.styleManager.RegisterCellStyle("BlockingVolumeIsNotAgree", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#b3675f" }));
            _jsGridParams.styleManager.RegisterCellStyle("BlockingVolumeSend", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#d4ceab" }));
            _jsGridParams.styleManager.RegisterCellStyle("BlockingVolumeChange", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#8a8787" }));
            _jsGridParams.styleManager.RegisterCellStyle("IsCommentDate", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#ffd1d1" }));
            _jsGridParams.styleManager.RegisterCellStyle("ConditionalFormatting", SP.JsGrid.Style.CreateStyle(SP.JsGrid.Style.Type.Cell, { backgroundColor: "#f8cbad" }));



            _jsGridControl.SetDelegate(SP.JsGrid.DelegateType.GetRecordEditMode, function (record) {

                if (record.properties.IsAssn.dataValue)
                    return SP.JsGrid.EditMode.ReadWriteDefer;
                else
                    return SP.JsGrid.EditMode.ReadOnly;
            });

            _jsGridControl.SetDelegate(SP.JsGrid.DelegateType.GetGridRowStyleId, function (record) {

                if (record.fieldRawDataMap.IsTask)
                    return "IsTask";
                else if (record.fieldRawDataMap.IsNewTask)
                    return "IsNewTask";
                else
                    return null;
            });



            _jsGridControl.AttachEvent(SP.JsGrid.EventType.OnCellEditCompleted, function (record) {
                if (lock == 0 && record.fieldKey == "PercentCompleteWork") {
                    lock = 1;
                    var newValue = "0%";
                    var value = _jsGridControl.GetRecord(record.recordKey).properties[record.fieldKey].localizedValue;
                    if (value != undefined && value != null && value != "") {
                        value = (value.replace("%", "")) * 1;
                        if (value != NaN && value > 0 && value <= 100)
                            newValue = value + "%";
                    }
                    var update = SP.JsGrid.CreateUnvalidatedPropertyUpdate(record.recordKey, record.fieldKey, newValue, false);
                    _jsGridControl.UpdateProperties([update], SP.JsGrid.UserAction.UserEdit);
                    lock = 0;
                }
            });


            //Инициализация настройки каждой ячейки
            InitColumns(_jsGridParams.tableViewParams.pivotedGridParams == null ? null : _jsGridParams.tableViewParams.pivotedGridParams.columns, _jsGridParams.tableViewParams.gridFieldMap, _jsGridParams.tableViewParams.columns);
            if (!_jsGridControl.IsInitialized()) {
                _jsGridParams.minHeaderHeight = 52;
                _jsGridControl.Init(_jsGridParams);
            }
            else
                _jsGridControl.SetTableView(_jsGridParams.tableViewParams);

        }
        else {
            $("div[id$='_ctl00_JsGridControl_disable_banner']").next().html("<p style='text-align: center; vertical-align: middle;'>Нет данных</p>");
            return;
        }

        _jsGridControl.SetSplitterPosition(1196);

        if (typeof (_jsGridParams.tableViewParams.ganttParams) != 'undefined' && _jsGridParams.tableViewParams.ganttParams != null) {

            _jsGridControl.SetGanttZoomLevel(_ganttZoomLevel);

            var tabH = $("div[id$='_ctl00_JsGridControl_rightpane']")[0].children[0];
            if (typeof (tabH) != 'undefined' && tabH != null)
                $(tabH).attr('style', 'height: 52px !important; background-color: #4169E1 !important; overflow: hidden; border-bottom-color: rgb(171, 171, 171); border-bottom-width: 1px; border-bottom-style: solid; position: relative;');

            var tabHc = $("div[id$='_ctl00_JsGridControl_rightpane'] div:first-child div");
            for (var i = 0; i < tabHc.length; i++) {
                var item = tabHc[i];
                var st = $(item).attr('style');
                st += " color: white !important;";
                $(item).attr('style', st);
            }
            $('.scroll-bar-grip').css('color', '#444444');
            var second = $("div[id$='_ctl00_JsGridControl_rightpane'] div:first-child")[0].children[1];
            if (second != null) {
                $(second).attr('style', 'left: 0px; top: 1px; width: 10000px; height: 30px; color: white !important; overflow: hidden; margin-left: -144px; border-bottom-color: rgb(198, 198, 198); border-bottom-width: 1px; border-bottom-style: solid; white-space: nowrap; position: absolute; background-color: transparent;');
            }
            var third = $("div[id$='_ctl00_JsGridControl_rightpane'] div:first-child")[0].children[2];
            if (third != null) {
                $(third).attr('style', 'left: 0px; top: 33px; width: 10000px; height: 17px; color: white !important; overflow: hidden; margin-left: 0px; border-bottom-color: rgb(198, 198, 198); border-bottom-width: 1px; border-bottom-style: solid; white-space: nowrap; position: absolute; background-color: transparent;');
            }
            var hgantt = $(".jsgrid-gantt-vert-today");
            if (hgantt != null) {
                $(hgantt).attr('style', 'height: 0px !important; overflow: hidden; border-bottom-color: rgb(171, 171, 171); border-bottom-width: 1px; border-bottom-style: solid; position: relative; background-color: rgb(65, 105, 225) !important;');
            }

        }


    }




    this.UrlCombine = function (path1, path2) {
        var path1EndsWith = path1.endsWith('/');
        var path2StartsWith = path2.indexOf('/') == 0;
        if (path1EndsWith && !path2StartsWith || !path1EndsWith && path2StartsWith) {
            return path1 + path2;
        }
        if (path1EndsWith && path2StartsWith) {
            return path1 + path2.slice(1);
        }
        return path1 + '/' + path2;
    }
    this.DoGETRequest = function (serviceUri, methodName, json, successFunc, errorFunc) {
        var url = window.location.protocol + "//" + window.location.host + _spPageContextInfo.siteServerRelativeUrl;
        $.ajax({
            type: 'GET',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: json,
            processData: false,
            cache: false,
            url: this.UrlCombine(url, this.UrlCombine(serviceUri, methodName)),
            context: document.body,
            success: successFunc,
            error: errorFunc
        });
    }
    this.DoPostRequest = function (serviceUri, methodName, json, successFunc, errorFunc) {
        var url = window.location.protocol + "//" + window.location.host + _spPageContextInfo.siteServerRelativeUrl;
        $.ajax({
            type: 'POST',
            //contentType: "application/json",
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cache: false,
            async: false,
            //dataType: 'json',
            data: json,
            processData: false,
            cache: false,
            url: this.UrlCombine(url, this.UrlCombine(serviceUri, methodName)),
            context: document.body,
            success: successFunc,
            error: errorFunc
        });
    }



    this.Grouping = function () {
        var element = document.getElementById(_GridPrefix + "f_Grouping");
        var elementLayout = document.getElementById(_GridPrefix + "f_Layout");
        if (typeof (element) != 'undefined' && element != null
            && typeof (elementLayout) != 'undefined' && elementLayout != null) {

            if (elementLayout.value == "GanttChart")
                _propertiesFilter["Layout"] = 1;
            else if (elementLayout.value == "PlanFact")
                _propertiesFilter["Layout"] = 2;
            else
                _propertiesFilter["Layout"] = 0;

            BindJSGrid(element.value);
        }
    }

    this.ViewLayoutMenu = function (type) {
        try {
            CloseAllMenu();
            _propertiesFilter["Layout"] = type;
            BindJSGrid(CMD_GetByFilters);
        }
        catch (e) {
            console.log(e);
        }

    }

    this.ViewScaleMenu = function (type) {
        try {
            CloseAllMenu();
            var height = "800";
            if (type == 0)
                height = "600";

            var url = window.location.href;

            if (url.indexOf("?") > -1)
                url = window.location.href.split('?')[0];

            url += "?height=" + height;


            var popupDiv = document.createElement('div');
            popupDiv.innerHTML = "<div>" +
                "Внимание. Изменение маштаба перезагрузит модуль и изменения будут сброшены." +
                "</div>" +
                "<div>" +
                "Продолжить?" +
                "</div>";
            popupDiv.innerHTML += '<div style="display:inline-flex; float: right;"><button type="button" style="float:right; margin-bottom:5px; margin-top:15px;" onclick="SP.UI.ModalDialog.commonModalDialogClose(SP.UI.DialogResult.OK);">ОК</button><button type="button" style="float:right; margin-bottom:5px; margin-top:15px;" onclick="SP.UI.ModalDialog.commonModalDialogClose(SP.UI.DialogResult.Cancel);">Отмена</button></div>';
            var options = {
                title: "Изменение маштаба",
                width: 345,
                height: 100,
                showClose: true,
                html: popupDiv.cloneNode(true),
                dialogReturnValueCallback:
                    function (res) {
                        if (res == 1) {
                            window.location.href = url;
                        }

                    }
            };
            SP.UI.ModalDialog.showModalDialog(options);

        }
        catch (e) {
            console.log(e);
        }

    }

    this.SendApproval = function () {
        try {
            CloseAllMenu();
            var checkedRecordKeys = _jsGridControl.GetCheckSelectionManager().GetCheckedRecordKeys();
            if (Object.keys(checkedRecordKeys).length == 0) {
                console.log("Задачи не выбраны");
                return;
            }
            var diffTracker = _jsGridControl.GetDiffTracker();
            var changesToSave = diffTracker.GetUniquePropertyChanges();
            for (key in checkedRecordKeys) {

                if (changesToSave[key] != null && changesToSave[key] != undefined) {
                    var dic = _jsGridControl.GetRecord(key).properties;
                    changesToSave[key]["TaskUid"] = dic["TaskUid"].localizedValue;
                    if (changesToSave[key]["PercentCompleteWork"] != undefined) {
                        if (changesToSave[key]["Start"] == undefined) {
                            var nd = new SP.JsGrid.Internal.PropertyUpdate();
                            nd.data = dic["Start"].dataValue;
                            nd.localized = dic["Start"].localizedValue;
                            changesToSave[key]["Start"] = nd;
                        }
                    }
                    else if (changesToSave[key]["Finish"] != undefined && changesToSave[key]["Start"] == undefined) {
                        var nd = new SP.JsGrid.Internal.PropertyUpdate();
                        nd.data = dic["Start"].dataValue;
                        nd.localized = dic["Start"].localizedValue;
                        changesToSave[key]["Start"] = nd;
                    }
                    _submittedApproval[key] = changesToSave[key];
                    console.log(key);
                }

            }

            BindJSGrid(CMD_Submitted_Approval);
            _submittedApproval = {};
        }
        catch (e) {
            console.log(e);
        }
    };

    this.SendAllApproval = function () {
        try {
            CloseAllMenu();
            var diffTracker = _jsGridControl.GetDiffTracker();
            var changesToSave = diffTracker.GetUniquePropertyChanges();
            if (changesToSave == null) {
                console.log("Нет изменений");
                return;
            }
            for (key in changesToSave) {
                var dic = _jsGridControl.GetRecord(key).properties;
                changesToSave[key]["TaskUid"] = dic["TaskUid"].localizedValue;
                _submittedApproval = changesToSave;
            }
            BindJSGrid(CMD_Submitted_Approval);
            _submittedApproval = {};
        }
        catch (e) {
            console.log(e);
        }
    };

    this.RunViewPerformance = function (performance) {
        try {
            CloseAllMenu();

            var a = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[0].children[0].children[0];
            $(a).html(_targetViewName);
            if (_targetViewName != performance) {
                _kindWorks = [];
                _kindWorksOut = [];
                _blok = [];
                _bloksOut = [];
                _blok2 = [];
                _bloks2Out = [];
                _capture = [];
                _captureOut = [];
                _floor = [];
                _floorOut = [];
                _filterTasks = [];
                _targetBasePlan = null;
                _selectedProjUid = null;
                _filterTasksOut = [];
                _targetViewName = performance;
                ClearSelectionAndLoadNew();
            }
            BindJSGrid(CMD_GetView);
        }
        catch (e) {
            console.log(e);
        }

    };




    this.ExportExcel = function () {
        var dt = new Date();
        var dtFormatted = ('0' + dt.getDate()).slice(-2) + '.' + ('0' + (dt.getMonth() + 1)).slice(-2) + '.' + dt.getFullYear();
        var filename = dtFormatted + "_project.xlsx";
        if (_selectedProjUid != undefined) {
            fetchExcel().then(function (resp) {
                var blob = resp.response;
                if (window.navigator && window.navigator.msSaveOrOpenBlob) {
                    window.navigator.msSaveOrOpenBlob(blob, filename);
                } else { //other browsers
                    var url = window.URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.style.display = 'none';
                    a.href = url;
                    a.download = filename;
                    document.body.appendChild(a);
                    a.click();
                    window.URL.revokeObjectURL(url);
                }
            })
                .catch(function (e) { console.log(e) });
        }
        else {
            alert("Выберите проект!");
        }
    };

    function fetchExcel() {
        var url = _spPageContextInfo.webAbsoluteUrl + "/_vti_bin/Legenda/ProjSpace/WebService.svc/ExportProjectToExcel";
        return new Promise(function (resolve, reject) {
            var xhr = new XMLHttpRequest(); setTimeout(function () {
                xhr.open("POST", url, true);
                xhr.setRequestHeader("Content-type", "application/json; charset=UTF-8");
                xhr.responseType = 'blob';
                xhr.send(JSON.stringify({ filter: { ProjUid: _selectedProjUid, ViewName: _targetViewName, TaskUids: _filterTasksOut, KindWork: _kindWorksOut, Stages: _floorOut, Bloks: _bloksOut }, type: 0 }));
                xhr.addEventListener('readystatechange', function (e) {
                    if (xhr.readyState != 4) return;
                    if (xhr.status == 200) {
                        resolve(xhr);
                    } else {
                        reject(xhr.statusText);
                    }
                });
            }, 10);
        });
    }


    this.ApplyFilter = function () {
        try {
            CloseAllMenu();
            BindJSGrid(CMD_GetByFilters);
        }
        catch (e) {
            console.log(e);
        }
    };

    this.ClearFilter = function () {
        try {
            CloseAllMenu();
            ClearSelection();
            BindJSGrid(CMD_GetByFilters);
        }
        catch (e) {
            console.log(e);
        }
    };

    this.SaveViewModal = function () {
        try {
            CloseAllMenu();
            var popupDiv = document.createElement('div');
            popupDiv.innerHTML = "<table cellpadding='5px' cellspacing='2px'>" +
                "<tr>" +
                "<td>" +
                "<span>Название представления:</span>" +
                "</td>" +
                "<td style=''>" +
                "<span>" +
                "<input type='text' id='createdViewName' value=''/>" +
                "</span>" +
                "</td>" +
                "</tr>" +
                "</table>";
            popupDiv.innerHTML += '<button type="button" style="float:right; margin-bottom:5px; margin-right:15px; margin-top:15px;" onclick="SP.UI.ModalDialog.commonModalDialogClose(SP.UI.DialogResult.OK, $(\'#createdViewName\').val());">Сохранить</button>';
            var options = {
                title: "Сохранение нового представления",
                width: 345,
                height: 100,
                showClose: true,
                html: popupDiv.cloneNode(true),
                dialogReturnValueCallback:
                    function (res, retVal) {
                        if (retVal != "" && res == 1) {
                            if (retVal == "Все задачи")
                                alert('Представление не будет сохранено! "Все задачи" - стандартное представление');
                            else
                                WGM.SaveView(retVal);
                        }

                    }
            };
            SP.UI.ModalDialog.showModalDialog(options);
        }
        catch (e) {
            console.log(e);
        }
    };

    this.RunDataBasePlan = function (basePlan) {
        try {
            CloseAllMenu();

            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[0].children[0].children[0];
            var textA = "Базовый план";
            if (basePlan != undefined && basePlan != null) {
                _targetBasePlan = basePlan * 1;
                if (_targetBasePlan > 0)
                    textA += " " + _targetBasePlan;
            }
            else
                textA = "Выбрать ... ";

            $(a).html(textA);
            _targetBasePlan = basePlan;
            //    BindJSGrid(CMD_GetByFilters);
        }
        catch (e) {
            console.log(e);
        }

    };

    this.NumTimeScale = function (timeScale) {

        if (_propertiesFilter["Layout"] == 1) {
            switch (timeScale) {
                case 0:
                    _jsGridControl.SetGanttZoomLevel(3);
                    _ganttZoomLevel = 3;
                    break;
                case 1:
                    _jsGridControl.SetGanttZoomLevel(5);
                    _ganttZoomLevel = 5;
                    break;
                case 2:
                    _jsGridControl.SetGanttZoomLevel(7);
                    _ganttZoomLevel = 7;
                    break;
                case 3:
                    _jsGridControl.SetGanttZoomLevel(8);
                    _ganttZoomLevel = 8;
                    break;
                case 4:
                    _jsGridControl.SetGanttZoomLevel(9);
                    _ganttZoomLevel = 9;
                    break;
                default:
                    _jsGridControl.SetGanttZoomLevel(3);
                    _ganttZoomLevel = 3;
                    break;
            }
        }

        switch (timeScale) {
            case 0:
                return "День";
            case 1:
                return "Неделя";
            case 2:
                return "Месяц";
            case 3:
                return "Квартал";
            case 4:
                return "Год";
            default:
                return "День";
        }
    }

    this.RunViewTimeScale = function (timeScale) {
        try {
            CloseAllMenu();
            _targetTimeScale = timeScale * 1;
            var a = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[2].children[0].children[0];
            var textA = WGM.NumTimeScale(_targetTimeScale);
            $(a).html(textA);
            // alert("Выбрана шкала времени: " + timeScale);
            BindJSGrid(CMD_GetByFilters);
        }
        catch (e) {
            console.log(e);
        }

    };

    this.FilterSelectProject = function (uidProject) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
        var selProj = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[0].children[0].children[0];
        $(menu).html("");
        for (var i = 0; i < _projects.length; i++) {
            if (_projects[i].Id == uidProject)
                $(selProj).html(_projects[i].Name);
        }
        if (_selectedProjUid != uidProject) {
            _kindWorks = [];
            _kindWorksOut = [];
            _blok = [];
            _bloksOut = [];
            _blok2 = [];
            _bloks2Out = [];
            _capture = [];
            _captureOut = [];
            _floor = [];
            _floorOut = [];
            _filterTasks = [];
            _targetBasePlan = null;
            _selectedProjUid = uidProject;
            _filterTasksOut = [];
            ClearSelectionAndLoadNew();
        }
        _selectedProjUid = uidProject;

        BindJSGrid(CMD_GetByFilters);

    };

    this.FilterSelectTask = function (item) {
        try {
            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[2].children[0].children[0];
            var textA = "";
            if (item == undefined) {
                _filterTasksOut = [];
                textA = "Выбрать ... ";
                CloseAllMenu();
            }
            else {
                var input = $(item)[0].children[0].children[0].children[0];
                var index = _filterTasksOut.indexOf(item.id);
                if ($(input)[0].checked == true) {
                    $(input)[0].checked = false;

                    for (var i = 0; i < _filterTasks.length; i++) {
                        var task = _filterTasks[i];
                        if ((task.Name.toLowerCase()).indexOf(item.name.toLowerCase()) > -1) {
                            if (_filterTasksOut.indexOf(task.Id) > -1)
                                _filterTasksOut.splice(index, 1);
                        }
                    }


                }
                else {
                    $(input)[0].checked = true;
                    for (var i = 0; i < _filterTasks.length; i++) {
                        var task = _filterTasks[i];
                        if ((task.Name.toLowerCase()).indexOf(item.name.toLowerCase()) > -1) {
                            if (_filterTasksOut.indexOf(task.Id) == -1)
                                _filterTasksOut.push(task.Id);
                        }
                    }
                }

                if (_filterTasksOut.length > 0) {
                    var unicTasks = [];
                    for (var i = 0; i < _filterTasksOut.length; i++) {
                        var id = _filterTasksOut[i];
                        var name = '';
                        for (var j = 0; j < _filterTasks.length; j++) {
                            if (id == _filterTasks[j].Id && unicTasks.indexOf(_filterTasks[j].Name) == -1)
                                unicTasks.push(_filterTasks[j].Name);
                        }
                    }
                    textA = "Выбрано: " + WGM.NumberOfSelectedTasks();
                }
                else
                    textA = "Выбрать ... ";

            }
            $(a).val(textA);
        }
        catch (e) {
            console.log(e);
        }

    };

    this.NumberOfSelectedTasks = function () {
        var unicTasks = [];
        for (var i = 0; i < _filterTasksOut.length; i++) {
            var id = _filterTasksOut[i];
            var name = '';
            for (var j = 0; j < _filterTasks.length; j++) {
                if (id == _filterTasks[j].Id && unicTasks.indexOf(_filterTasks[j].Name) == -1)
                    unicTasks.push(_filterTasks[j].Name);
            }
        }
        return unicTasks.length;
    };

    this.RunDataTypeWork = function (item) {
        try {

            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[4].children[0].children[0];
            var textA = "";
            if (item == undefined) {
                _kindWorksOut = [];
                textA = "Выбрать ... ";
                CloseAllMenu();
            }
            else {
                var input = $(item)[0].children[0].children[0].children[0];
                var index = _kindWorksOut.indexOf(item.id);
                if ($(input)[0].checked == true) {
                    $(input)[0].checked = false;
                    if (_kindWorksOut.indexOf(item.id) > -1)
                        _kindWorksOut.splice(index, 1);
                }
                else {
                    $(input)[0].checked = true;
                    if (_kindWorksOut.indexOf(item.id) == -1)
                        _kindWorksOut.push(item.id);
                }

                _kindWorksOut = _kindWorksOut.sort((a, b) => a.localeCompare(b));

                for (var i = 0; i < _kindWorksOut.length; i++) {
                    textA += _kindWorksOut[i];
                    if (i < _kindWorksOut.length - 1)
                        textA += "; ";
                }

                if (_kindWorksOut.length == 0)
                    textA = "Выбрать ... ";
            }
            $(a).html(textA);

        }
        catch (e) {
            console.log(e);
        }
    };

    this.RunDataFloor = function (item) {
        try {
            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[2].children[0].children[0];
            var textA = "";
            if (item == undefined) {
                _floorOut = [];
                textA = "Выбрать ... ";
                CloseAllMenu();
            }
            else {
                var input = $(item)[0].children[0].children[0].children[0];
                var index = _floorOut.indexOf(item.id);
                if ($(input)[0].checked == true) {
                    $(input)[0].checked = false;
                    if (index > -1)
                        _floorOut.splice(index, 1);
                }
                else {
                    $(input)[0].checked = true;
                    if (index == -1)
                        _floorOut.push(item.id);
                }

                _floorOut = _floorOut.sort((a, b) => a.localeCompare(b));

                for (var i = 0; i < _floorOut.length; i++) {
                    textA += _floorOut[i];
                    if (i < _floorOut.length - 1)
                        textA += "; ";
                }

                if (_floorOut.length == 0)
                    textA = "Выбрать ... ";
            }
            $(a).html(textA);





        }
        catch (e) {
            console.log(e);
        }
    };

    this.RunDataBlok = function (item) {
        try {
            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[4].children[0].children[0];
            var textA = "";
            if (item == undefined) {
                _bloksOut = [];
                textA = "Выбрать ... ";
                CloseAllMenu();
            }
            else {
                var input = $(item)[0].children[0].children[0].children[0];
                var index = _bloksOut.indexOf(item.id);
                if ($(input)[0].checked == true) {
                    $(input)[0].checked = false;
                    if (index > -1)
                        _bloksOut.splice(index, 1);
                }
                else {
                    $(input)[0].checked = true;
                    if (index == -1)
                        _bloksOut.push(item.id);
                }
                //console.log(id);
            }

            _bloksOut = _bloksOut.sort((a, b) => a.localeCompare(b));

            for (var i = 0; i < _bloksOut.length; i++) {
                textA += _bloksOut[i];
                if (i < _bloksOut.length - 1)
                    textA += "; ";
            }

            if (_bloksOut.length == 0)
                textA = "Выбрать ... ";

            $(a).html(textA);
        }
        catch (e) {
            console.log(e);
        }
    };

    this.RunDataBlok2 = function (item) {
        //return;
        try {
            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[0].children[0].children[0];
            var textA = "";
            if (item == undefined) {
                _bloks2Out = [];
                textA = "Выбрать ... ";
                CloseAllMenu();
            }
            else {
                var input = $(item)[0].children[0].children[0].children[0];
                var index = _bloks2Out.indexOf(item.id);
                if ($(input)[0].checked == true) {
                    $(input)[0].checked = false;
                    if (index > -1)
                        _bloks2Out.splice(index, 1);
                }
                else {
                    $(input)[0].checked = true;
                    if (index == -1)
                        _bloks2Out.push(item.id);
                }
                //console.log(id);
            }

            _bloks2Out = _bloks2Out.sort((a, b) => a.localeCompare(b));

            for (var i = 0; i < _bloks2Out.length; i++) {
                textA += _bloks2Out[i];
                if (i < _bloks2Out.length - 1)
                    textA += "; ";
            }

            if (_bloks2Out.length == 0)
                textA = "Выбрать ... ";

            $(a).html(textA);
        }
        catch (e) {
            console.log(e);
        }
    };

    this.RunDataCapture = function (item) {
        //return;
        try {
            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[2].children[0].children[0];
            var textA = "";
            if (item == undefined) {
                _captureOut = [];
                textA = "Выбрать ... ";
                CloseAllMenu();
            }
            else {
                var input = $(item)[0].children[0].children[0].children[0];
                var index = _captureOut.indexOf(item.id);
                if ($(input)[0].checked == true) {
                    $(input)[0].checked = false;
                    if (index > -1)
                        _captureOut.splice(index, 1);
                }
                else {
                    $(input)[0].checked = true;
                    if (index == -1)
                        _captureOut.push(item.id);
                }
                //console.log(id);
            }

            _captureOut = _captureOut.sort((a, b) => a.localeCompare(b));

            for (var i = 0; i < _captureOut.length; i++) {
                textA += _captureOut[i];
                if (i < _captureOut.length - 1)
                    textA += "; ";
            }

            if (_captureOut.length == 0)
                textA = "Выбрать ... ";

            $(a).html(textA);
        }
        catch (e) {
            console.log(e);
        }
    };


    this.NumSchedulePeriod = function (schedulePeriod) {
        switch (schedulePeriod) {
            case 0:
                return "Текущая неделя";
            case 1:
                return "Текущий месяц";
            case 2:
                return "Текущий квартал";
            case 3:
                return "Выбрать период";
            case 4:
                return "Весь период";
            default:
                return "Весь период";
        }
    }

    this.RunViewSchedulePeriod = function (schedulePeriod) {
        try {
            CloseAllMenu();
            _schedulePeriod = schedulePeriod * 1;
            var a = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[4].children[0].children[0];
            var textA = WGM.NumSchedulePeriod(_schedulePeriod);

            if (_schedulePeriod == 3) {
                var heightModal = 100;
                var dataS = "";
                if (_periodStart != null && _periodStart != undefined) {
                    dataS = _periodStart.split("T")[0];
                    var dataSplit = dataS.split("-");
                    dataS = dataSplit[2] + "." + dataSplit[1] + "." + dataSplit[0];
                }
                var dataF = "";
                if (_periodFinish != null && _periodFinish != undefined) {
                    dataF = _periodFinish.split("T")[0];
                    var dataSplit = dataF.split("-");
                    dataF = dataSplit[2] + "." + dataSplit[1] + "." + dataSplit[0];
                }
                var popupDiv = document.createElement('div');
                popupDiv.innerHTML = "<table cellpadding='5px' cellspacing='2px'>" +
                    "<tr>" +
                    "<td>" +
                    "Начало:" +
                    "</td>" +
                    "<td style=''>" +
                    "<span>" +
                    "<input type='text' id='createdBeforeDateStart' value='" + dataS + "'/>" +
                    "<iframe id='createdBeforeDateStartDatePickerFrame' title='Select a date from the calendar.' style='display:none; position:absolute; width:200px; z-index:101;' src='/_layouts/15/images/blank.gif?rev=23' class='owl-date-picker'></iframe>" +
                    "</span>" +
                    "<span style='vertical-align: middle;'>" +
                    "<a role='button' onclick='clickDatePicker(\"createdBeforeDateStart\", \"/_layouts/15/iframe.aspx?&cal=1&lcid=1049&langid=1049&tz=-08:00:00.0002046&ww=0111110&fdow=0&fwoy=0&hj=0&swn=false&minjday=109207&maxjday=2666269&date=\", \"\", event); return false;' href='#'>" +
                    "<img id='createdBeforeDateDatePickerImageStart' alt='Select a date from the calendar.' src='/_layouts/15/images/calendar_25.gif?rev=23' border='0' style=''/>" +
                    "</a>" +
                    "</span>" +
                    "</td>" +
                    "<td>" +
                    "Окончание:" +
                    "</td>" +
                    "<td style=''>" +
                    "<span>" +
                    "<input type='text' id='createdBeforeDateFinish' value='" + dataF + "'/>" +
                    "<iframe id='createdBeforeDateFinishDatePickerFrame' title='Select a date from the calendar.' style='display:none; position:absolute; width:200px; z-index:101;' src='/_layouts/15/images/blank.gif?rev=23' class='owl-date-picker'></iframe>" +
                    "</span>" +
                    "<span style='vertical-align: middle;'>" +
                    "<a role='button' onclick='clickDatePicker(\"createdBeforeDateFinish\", \"/_layouts/15/iframe.aspx?&cal=1&lcid=1049&langid=1049&tz=-08:00:00.0002046&ww=0111110&fdow=0&fwoy=0&hj=0&swn=false&minjday=109207&maxjday=2666269&date=\", \"\", event); return false;' href='#'>" +
                    "<img id='createdBeforeDateFinish' alt='Select a date from the calendar.' src='/_layouts/15/images/calendar_25.gif?rev=23' border='0' style=''/>" +
                    "</a>" +
                    "</span>" +
                    "</td>" +
                    "</tr>" +
                    "</table>";
                popupDiv.innerHTML += '<button type="button" style="float:right; margin-bottom:5px; margin-right:5px; margin-top:15px;" onclick="SP.UI.ModalDialog.commonModalDialogClose(SP.UI.DialogResult.OK,  [$(\'#createdBeforeDateStart\').val(),$(\'#createdBeforeDateFinish\').val()]);">Отправить</button>';
                var options = {
                    title: "Выберете период",
                    width: 599,
                    height: heightModal,
                    showClose: true,
                    html: popupDiv.cloneNode(true),
                    dialogReturnValueCallback:
                        function (res, retVal) {

                            var ad = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[4].children[0].children[0];
                            var text = "";
                            if (retVal.length == 2 && retVal[0] != "" && retVal[1] != "") {
                                text = retVal[0] + "; " + retVal[1];
                                var dataTokensStart = retVal[0].split(".");
                                var dataTokensFinish = retVal[1].split(".");
                                _periodStart = (new Date(+dataTokensStart[2], +dataTokensStart[1] - 1, +dataTokensStart[0])).toUTCString();
                                _periodFinish = (new Date(+dataTokensFinish[2], +dataTokensFinish[1] - 1, +dataTokensFinish[0])).toUTCString();
                                $(ad).html(text);
                                BindJSGrid(CMD_GetByFilters);
                            }


                        }
                };
                SP.UI.ModalDialog.showModalDialog(options);

            }
            else {
                $(a).html(textA);
                BindJSGrid(CMD_GetByFilters);
            }


        }
        catch (e) {
            console.log(e);
        }

    };

    this.SaveView = function (viewName) {
        try {
            _gridColumn = InitializeJSGridColumnsSettings();
            _jsGridParams.tableViewParams.paneLayout;
            _propertiesFilter["Layout"] = _jsGridParams.tableViewParams.paneLayout;
            _newViewName = viewName;
            BindJSGrid(CMD_SaveView);
        }
        catch (e) {
            console.log(e);
        }
    }



    this.Layout = function (type) {
        _propertiesFilter["Layout"] = type;
        var element = document.getElementById(_GridPrefix + "f_Grouping");
        if (typeof (element) != 'undefined' && element != null) {
            BindJSGrid(element.value);
        }
    }

    var globalNotificationID = '';
    var globalStatusID = '';

    function InitColumns(columns, gridFieldMap, allcolumns) {
        if (columns == null)
            columns = allcolumns.GetColumnArray();
        else
            columns = columns.GetColumnArray();

        var allcolumnsArray = allcolumns.GetColumnArray();
        for (var i = 0; i < allcolumnsArray.length; i++) {
            var acolumn = allcolumnsArray[i];
            if (acolumn.columnKey.indexOf("PercentCompleteWork") == 0)
                acolumn.fnGetCellStyleId = function (record, fieldKey, dataValue) {
                    var thisDate = new Date();
                    var now = new Date(thisDate.getFullYear(), thisDate.getMonth(), thisDate.getDate());
                    var start = new Date(record.fieldRawDataMap.Start.getFullYear(), record.fieldRawDataMap.Start.getMonth(), record.fieldRawDataMap.Start.getDate());
                    var finish = new Date(record.fieldRawDataMap.Finish.getFullYear(), record.fieldRawDataMap.Finish.getMonth(), record.fieldRawDataMap.Finish.getDate());
                    var percentStr = record.fieldRawLocMap.PercentCompleteWork;
                    if (percentStr != undefined && percentStr != "" && record.fieldRawDataMap.IsAssn) {
                        var percent = percentStr.replace("%", "") * 1;
                        if (start != undefined) {
                            if (start <= now && percent == 0)
                                return "ConditionalFormatting";
                        }

                        if (finish != undefined) {
                            if (finish > now && percent == 100)
                                return "ConditionalFormatting";
                            if (finish <= now && percent < 100)
                                return "ConditionalFormatting";
                        }
                    }

                    return null;
                }

            if (acolumn.columnKey.indexOf("Start") == 0) {
                acolumn.fnGetCellEditMode = function (record, fieldKey) {
                    if (record.properties["IsStartDateClosed"].dataValue) {
                        return SP.JsGrid.EditMode.ReadOnly;
                    }
                }
            }
        }

        for (var i = 0; i < columns.length; i++) {
            var column = columns[i];




            if (document.getElementById("gridVolumeViewType").value == "FormationValue" &&
                column.columnKey.indexOf("UnitPrice") == 0 &&
                (GetStage(_stage) > 2 && GetStage(_stage) < 6)) {
                column.fnGetCellStyleId = function (record, fieldKey, dataValue) {
                    return "BlockingUnitPrice";
                }
                column.fnGetCellEditMode = function (record, fieldKey) {
                    return SP.JsGrid.EditMode.ReadOnly;
                }
            }

            if (document.getElementById("gridVolumeViewType").value == "FormationValue" &&
                column.columnKey.indexOf("Title") == 0) {
                column.fnGetCellEditMode = function (record, fieldKey) {

                    if (record.properties.IsNewTask.dataValue)
                        return SP.JsGrid.EditMode.ReadWrite;
                    else
                        return SP.JsGrid.EditMode.ReadOnly;

                }


            }

            /*
            column.fnGetCellStyleId = function (record, fieldKey, dataValue) {
                if (record.properties.IsTask.dataValue)
                        return "IsTask";
                    else
                        return null;
            }
            */

        }

        for (var i = 0; i < columns.length; i++) {
            var IsClosed = false;
            var column = columns[i];
            if (column.columnKey.indexOf("_Date") == 0) {
                IsClosed = true;
                if (column.columnKey.indexOf("_Date0") == 0) {
                    var element_Button5 = document.getElementById(_GridPrefix + "Button5");
                    if (typeof (element_Button5) != 'undefined' && element_Button5 != null) { document.getElementById(_GridPrefix + "Button5").disabled = true; }
                }
            }

            if (column.columnKey.indexOf("_Date") == 0) {
                column.fnGetCellEditMode = function (record, fieldKey) {
                    if (fieldKey.indexOf("Forecast") != -1) {
                        if (record.fieldRawDataMap.i && record.fieldRawDataMap.i.htmlText != "" && fieldKey.indexOf("NewForecast") != -1)
                            return SP.JsGrid.EditMode.ReadOnlyDefer;
                        else
                            return null;
                    }
                    else {
                        if (!record.fieldRawDataMap.IsTask)
                            return SP.JsGrid.EditMode.ReadOnlyDefer;
                        else
                            return null;
                    }
                }
            }



            if (column.columnKey.indexOf("Sum") == 0) {
                column.fnGetCellEditMode = function (record, fieldKey) {
                    if (fieldKey.indexOf("SumFact") == 0 && record.properties["Measure"].localizedValue == "ч") {
                        return SP.JsGrid.EditMode.ReadOnly;
                    }
                }
            }



            if (column.columnKey.indexOf("Date") == 0) {

                column.fnGetCellStyleId = function (record, fieldKey, dataValue) {
                    //if (record.properties["IsCommentDate"].dataValue == true) {
                    //    var iscomn = false;
                    //    _commentGrid.forEach(function (element) {
                    //        if (IsNotNull(element.GuidTask) && element.GuidTask == record.recordKey && !iscomn) {
                    //            element.ListVolumeCommentsData.forEach(function (commentList) {
                    //                if (commentList.Data == fieldKey) { iscomn = true; }
                    //            });
                    //        }
                    //    });

                    //    if (iscomn) { return "IsCommentDate"; }
                    //}

                    if (fieldKey.indexOf("Forecast") != -1)
                        return null;
                    if (_changesToSave[record.recordKey] && _changesToSave[record.recordKey][fieldKey])
                        return "VolumeChange";
                    if (record.properties["Status" + fieldKey.replace("Date", "")].localizedValue == "0")
                        return null;
                    if (record.properties["Status" + fieldKey.replace("Date", "")].localizedValue == "1")
                        return "VolumeSend";
                    if (record.properties["Status" + fieldKey.replace("Date", "")].localizedValue == "2")
                        return "VolumeIsNotAgree";
                    if (record.properties["Status" + fieldKey.replace("Date", "")].localizedValue == "3")
                        return "VolumeIsAgree";
                }



                column.fnGetCellEditMode = function (record, fieldKey) {
                    if (fieldKey.indexOf("Forecast") != -1) {
                        if (record.fieldRawDataMap.i && record.fieldRawDataMap.i.htmlText != "" && fieldKey.indexOf("NewForecast") != -1)
                            return SP.JsGrid.EditMode.ReadWrite;
                        else
                            return null;
                    }
                    else if (fieldKey.indexOf("DateFact") == 0 && record.properties["Measure"].localizedValue == "ч") {
                        return SP.JsGrid.EditMode.ReadOnly;
                    }
                    else if (record.fieldRawDataMap.IsNewTask) {
                        if (record.fieldRawDataMap.i && record.fieldRawDataMap.i.htmlText != "" && fieldKey.indexOf("Title") != -1)
                            return SP.JsGrid.EditMode.ReadWriteDefer;
                        else
                            return SP.JsGrid.EditMode.ReadOnlyDefer
                    }
                    else {
                        if (!record.fieldRawDataMap.IsTask)
                            return SP.JsGrid.EditMode.ReadWrite;
                        else
                            return null;
                    }
                }
            }
            /*
                        column.fnGetCellStyleId = function (record, fieldKey, dataValue) {
                            if (record.properties.IsTask.dataValue)
                                    return "IsTask";
                                else
                                    return null;
                        }
                        */

        }



    }


    function RemoveNotification(globalNotificationID) {
        SP.UI.Notify.removeNotification(globalNotificationID);
        globalNotificationID = '';
    }

    function createTab() {
        var ribbon = SP.Ribbon.PageManager.get_instance().get_ribbon();

        if (ribbon !== null) {
            var tabId = "TaskReport.Id";
            var tabTasksGroupId = "TaskReport.Tasks.Id";
            var tabDataGroupId = "TaskReport.Data.Id";
            var tabViewGroupId = "TaskReport.View.Id";
            var tabExportGroupId = "TaskReport.Export.Id";
            var tabText = "Отчет по задачам";
            var group;

            var tab = new CUI.Tab(ribbon, tabId, tabText, "Tab Description", 'TaskReport', false, '', null, null);
            ribbon.addChild(tab);

            group = new CUI.Group(ribbon, tabTasksGroupId, "Задачи", "Group Description", 'TaskReport.Attach.Group.Tasks', null);
            tab.addChild(group);
            group = new CUI.Group(ribbon, tabDataGroupId, "Данные", "Group Description", 'TaskReport.Attach.Group.Data', null);
            tab.addChild(group);
            group = new CUI.Group(ribbon, tabViewGroupId, "Вид", "Group Description", 'TaskReport.Attach.Group.View', null);
            tab.addChild(group);
            group = new CUI.Group(ribbon, tabExportGroupId, "Экспорт", "Group Description", 'TaskReport.Attach.Group.Export', null);
            tab.addChild(group);

            //SelectRibbonTab("Ribbon.Read", true);
            SelectRibbonTab(tabId, true);
            /*
            
            */
            /*--------Задачи--------*/
            var sendAS = $('.ms-cui-tabContainer')[0].children[0].children[0].children[0].children[0];
            var aSendAS = '<a href="javascript:;" class="ms-cui-ctl-large " unselectable="on"  onclick = "MenuTaskReportTasks(); return false;"  role="button" aria-haspopup="true" id="Ri.TaskReport.Tasks" >' +
                '<span unselectable="on" class="ms-cui-ctl-largeIconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -256px; left: -160px;">' +
                '</span></span><span unselectable="on" class="ms-cui-ctl-largelabel">Отправить<br>состояние <span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float">' +
                '<img unselectable="on" alt="" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></span></a><span></span>';
            $(sendAS).html(aSendAS);
            /*--------Данные--------*/
            var dataAS = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0];
            var aDataFirstColl = '<table style=""><tr>' +
                '<td style="max-width: 70px;" class="TaskReportLabel">' +
                '<span style="top: -12px !important; position: relative; float: left;">Проект   </span><br/><span style="top: -3px !important; position: relative; float: left;">Задача   </span><br/><span style="top: 6px !important; position: relative; float: left;">Вид работ   </span>' +
                '</td>' +
                '<td style="max-width: 140px;">' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.Project.List" style="height: 21px !important;" ><span unselectable="on" id="Ri.TaskReport.Data.Project.Span" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveProject(); return false;"><a>';
            if (_selectedProjUid != undefined && _selectedProjUid != null && _selectedProjUid != "") {
                for (var i = 0; i < _projects.length; i++) {
                    if (_projects[i].Id == _selectedProjUid)
                        aDataFirstColl += _projects[i].Name;
                }

            }
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveProject(); return false;" id="Ri.TaskReport.Data.Project.Button"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.Task.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveTask(); return false;">' +
                '<input id="searchTask" autocapitalize="none" autocomplete="off" autocorrect="off" name="search_query" tabindex="0" type="text" spellcheck="false" role="combobox" oninput = "MenuActiveTaskInput()" aria-haspopup="false" aria-autocomplete="list" class="gsfi ytd-searchbox" dir="ltr" ' +
                'style="border: none; padding: 0px; margin: 0px; height: auto; width: 100%; outline: none; text-align:center;" value="';
            if (_filterTasksOut.length > 0)
                textA = "Выбрано: " + WGM.NumberOfSelectedTasks();
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '"></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveTask(); return false;" id="Ri.TaskReport.Data.Task"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.TypeWork.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveTypeWork(); return false;"><a>';
            if (_kindWorksOut.length > 0)
                for (var i = 0; i < _kindWorksOut.length; i++) {
                    aDataFirstColl += _kindWorksOut[i];
                    if (i < _kindWorksOut.length - 1)
                        aDataFirstColl += "; ";
                }
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveTypeWork(); return false;" id="Ri.TaskReport.Data.TypeWork"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '</td>' +
                '<td style="max-width: 80px;" class="TaskReportLabel">' +
                '<span style="top: -12px !important; position: relative; float: left;">Базовый план   </span><br/><span style="top: -3px !important; position: relative; float: left;">Этаж   </span><br/><span style="top: 6px !important; position: relative; float: left;">Блок   </span>' +
                '</td>' +
                '<td style="max-width: 140px;">' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.BasePlan.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveBasePlan(); return false;"><a>';
            if (_targetBasePlan != undefined && _targetBasePlan != null && _targetBasePlan != "") {
                var textBase = "Базовый план";
                var targetBasePlan = _targetBasePlan * 1;
                if (targetBasePlan > 0)
                    textBase += " " + targetBasePlan;
                aDataFirstColl += textBase;
            }
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveBasePlan(); return false;" id="Ri.TaskReport.Data.BasePlan"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.Floor.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveFloor(); return false;"><a>';
            if (_floorOut.length > 0)
                for (var i = 0; i < _floorOut.length; i++) {
                    aDataFirstColl += _floorOut[i];
                    if (i < _floorOut.length - 1)
                        aDataFirstColl += "; ";
                }
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveFloor(); return false;" id="Ri.TaskReport.Data.Floor"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.Block.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveBlok(); return false;"><a>';
            if (_bloksOut.length > 0)
                for (var i = 0; i < _bloksOut.length; i++) {
                    aDataFirstColl += _bloksOut[i];
                    if (i < _bloksOut.length - 1)
                        aDataFirstColl += "; ";
                }
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveBlok(); return false;" id="Ri.TaskApproval.Data.Block"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '</td>' +
                '<td style="max-width: 70px;" class="TaskReportLabel">' +
                // Block 2
                '<span style="top: -18px !important; position: relative; float: left;">Блок 2   </span><br/>' +
                '<span style="top: -10px !important; position: relative; float: left;">Захватки   </span><br/>' +
                '</td>' +
                '<td style="max-width: 140px; vertical-align: top">' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.Block2.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveBlok2(); return false;"><a>';
            if (_bloksOut.length > 0)
                for (var i = 0; i < _bloksOut.length; i++) {
                    aDataFirstColl += _bloksOut[i];
                    if (i < _bloksOut.length - 1)
                        aDataFirstColl += "; ";
                }
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveCapture(); return false;" id="Ri.TaskApproval.Data.Capture"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>';
            aDataFirstColl += '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.Data.Capture.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuActiveCapture(); return false;"><a>';
            if (_captureOut.length > 0)
                for (var i = 0; i < _captureOut.length; i++) {
                    aDataFirstColl += _captureOut[i];
                    if (i < _captureOut.length - 1)
                        aDataFirstColl += "; ";
                }
            else
                aDataFirstColl += 'Выбрать ... ';
            aDataFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuActiveCapture(); return false;" id="Ri.TaskApproval.Data.Block2"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>';
            // end Blok2
            aDataFirstColl += '</td>' +
                '<td>' +
                '<span unselectable="on" class="ms-cui-layout" id="Ribbon.TaskApproval.ApplyFilter-LargeLarge">' +
                '<span unselectable="on" class="ms-cui-section" id="Ribbon.TaskApproval.ApplyFilter-LargeLarge-0">' +
                '<span unselectable="on" class="ms-cui-row-onerow" id="Ribbon.TaskApproval.ApplyFilter-LargeLarge-0-0">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.ApplyFilter(); return false;" class="ms-cui-ctl-large " mscui:controltype="Button" role="button" id="Ri.TaskApproval.Data.ApplyFilter">' +
                '<span unselectable="on" class="ms-cui-ctl-largeIconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -97px; left: -160px;">' +
                '</span></span><span unselectable="on" class="ms-cui-ctl-largelabel">Применить</span></a>' +

                '<a unselectable="on" href="javascript:;" onclick="WGM.ClearFilter(); return false;" class="ms-cui-ctl-large " mscui:controltype="Button" role="button" id="Ri.TaskApproval.Data.ClearFilter">' +
                '<span unselectable="on" class="ms-cui-ctl-largeIconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -65px; left: -32px;">' +
                '</span></span><span unselectable="on" class="ms-cui-ctl-largelabel">Сбросить все</span></a>' +
                '</span></span></span>';
            aDataFirstColl += '</td></tr></table>';
            $(dataAS).html(aDataFirstColl);
            /*--------Вид--------*/
            var typeAS = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0];
            var aTypeFirstColl = '<table style=""><tr>' +
                '<td style="max-width: 150px;" class="TaskReportLabel">' +
                '<span style="top: -12px !important; position: relative; float: left;">Представление: </span><br/><span style="top: -3px !important; position: relative; float: left;">Шкала времени: </span><br/><span style="top: 6px !important; position: relative; float: left;">Период расписания: </span>' +
                '</td>' +
                '<td style="max-width: 140px;">' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.View.Performance.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuViewPerformance(); return false;"><a>';
            if (_targetViewName != undefined && _targetViewName != "")
                aTypeFirstColl += _targetViewName;
            else
                aTypeFirstColl += 'Выбрать ... ';
            aTypeFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuViewPerformance(); return false;" id="Ri.TaskReport.View.Performance"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.View.TimeScale.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuTaskReportViewTimeScale(); return false;"><a>';
            aTypeFirstColl += WGM.NumTimeScale(_targetTimeScale);
            aTypeFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuTaskReportViewTimeScale(); return false;" id="Ri.TaskReport.View.TimeScale"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '<span unselectable="on" class="ms-cui-dd " mscui:controltype="DropDown" id="Ri.TaskReport.View.SchedulePeriod.List" style="height: 21px !important;"><span unselectable="on" class="ms-cui-dd-text" style="width: 100px;" onclick="MenuaskReportViewSchedulePeriod(); return false;"><a>';
            aTypeFirstColl += WGM.NumSchedulePeriod(_schedulePeriod);
            aTypeFirstColl += '</a></span><a unselectable="on" role="button" aria-haspopup="true" class="ms-cui-dd-arrow-button" href="javascript:;" onclick="MenuaskReportViewSchedulePeriod(); return false;" id="Ri.TaskReport.View.SchedulePeriod"><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></a></span><span></span>' +
                '</td>' +

                '<td>' +
                '<a unselectable="on" href="javascript:;" onclick = "RiTaskReportViewLayout(); return false;" class="ms-cui-ctl-large " mscui:controltype="FlyoutAnchor" role="button" aria-haspopup="true" id="Ri.TaskReport.View.Layout"><span unselectable="on" class="ms-cui-ctl-largeIconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -352px; left: -320px;"></span></span><span unselectable="on" class="ms-cui-ctl-largelabel">Макет<br><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></span></a><span></span>' +
                '</td>' +

                '<td>' +
                '<a unselectable="on" href="javascript:;" onclick = "RiTaskReportScale(); return false;" class="ms-cui-ctl-large " mscui:controltype="FlyoutAnchor" role="button" aria-haspopup="true" id="Ri.TaskReport.View.Scale"><span unselectable="on" class="ms-cui-ctl-largeIconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -96px; left: -32px;"></span></span><span unselectable="on" class="ms-cui-ctl-largelabel">Масштаб<br><span unselectable="on" class=" ms-cui-img-5by3 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/formatmap16x16.png?rev=43" style="top: -35px; left: -27px;"></span></span></a><span></span>' +
                '</td>' +

                '<td>' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.SaveViewModal(); return false;" class="ms-cui-ctl-large" mscui:controltype="Button" role="button" id="Ri.TaskReport.View.SaveView"><span unselectable="on" class="ms-cui-ctl-largeIconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -32px; left: -320px;"></span></span><span unselectable="on" class="ms-cui-ctl-largelabel">Сохранить</span></a>' +
                '</td>' +

                '</tr></table>';
            $(typeAS).html(aTypeFirstColl);
            /*--------Экспорт--------*/
            var exportAs = $('.ms-cui-tabContainer')[0].children[0].children[3].children[0].children[0];
            var aExportAs = '<span unselectable="on" class="ms-cui-layout" id="Ribbon.TaskApproval.Export-LargeLarge">' +
                '<span unselectable="on" class="ms-cui-section" id="Ribbon.TaskApproval.Export-LargeLarge-0">' +
                '<span unselectable="on" class="ms-cui-row-onerow" id="Ribbon.TaskApproval.Export-LargeLarge-0-0">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.ExportExcel(); return false;" class="ms-cui-ctl-large " mscui:controltype="Button" role="button" id="Ri.TaskReport.Export.Excel">' +
                '<span unselectable="on" class="ms-cui-ctl-largeIconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -352px; left: -352px;">' +
                '</span></span><span unselectable="on" class="ms-cui-ctl-largelabel">Экспорт в<br>Excel</span></a>' +
                '</span></span>'; // export section

            aExportAs += '</span>';
            $(exportAs).html(aExportAs);

            //var g1 = $('.ms-cui-tabContainer')[0].children[0].children[0];
            //$(g1).css("width", 68.92);
            //var g2 = $('.ms-cui-tabContainer')[0].children[0].children[1];
            //$(g2).css("width", 559.17);
            //var g3 = $('.ms-cui-tabContainer')[0].children[0].children[2];
            //$(g3).css("width", 413);
            //var g4 = $('.ms-cui-tabContainer')[0].children[0].children[3];
            //$(g4).css("width", 59.41);


            $(".ms-cui-groupTitle, .TaskReportLabel").mouseup(function () {
                try {

                    CheckMenu();
                }
                catch (ex) {
                    console.log(ex);
                }
            });


        }


    }

    function ClearSelection() {
        try {
            var text = "Выбрать ... ";

            //Select Project
            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[0].children[0].children[0];
            $(a).html(text);
            _selectedProjUid = null;

            //Select Task
            var a0 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[2].children[0].children[0];
            $(a0).html(text);
            _filterTasksOut = [];
            $('#searchTask').val(text);

            //Select TypeWork
            var a1 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[4].children[0].children[0];
            $(a1).html(text);
            _kindWorksOut = [];

            //Select BasePlan
            var a2 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[0].children[0].children[0];
            $(a2).html(text);
            targetBasePlan = null;

            //Select Floor
            var a3 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[2].children[0].children[0];
            $(a3).html(text);
            _floorOut = [];

            //Select Blok
            var a4 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[4].children[0].children[0];
            $(a4).html(text);
            _bloksOut = [];

            //Select Blok2
            var a41 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[0].children[0].children[0];
            $(a41).html(text);
            _bloks2Out = [];

            //Select Capture
            var a42 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[2].children[0].children[0];
            $(a42).html(text);
            _captureOut = [];


            //Select ViewPerformance
            var a5 = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[0].children[0].children[0];
            $(a5).html(text);
            if (_targetViewName != undefined && _targetViewName != "")
                $(a5).html(_targetViewName);

            //Select TimeScale
            var a6 = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[2].children[0].children[0];
            text = WGM.NumTimeScale(_targetTimeScale);
            $(a6).html(text);

            //Select SchedulePeriod
            var a7 = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[4].children[0].children[0];
            text = WGM.NumSchedulePeriod(_schedulePeriod);
            $(a7).html(text);

        }
        catch (e) {
            console.log(e);
        }

    }








    function ClearSelectionAndLoadNew() {
        try {
            var text = "Выбрать ... ";

            //Select Project
            var a = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[0].children[0].children[0];
            $(a).html(text);
            if (_selectedProjUid != undefined && _selectedProjUid != null && _selectedProjUid != "") {
                for (var i = 0; i < _projects.length; i++) {
                    if (_projects[i].Id == _selectedProjUid)
                        $(a).html(_projects[i].Name);
                }
            }
            //Select Task
            var a0 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[2].children[0].children[0];
            $(a0).html(text);
            if (_filterTasksOut.length > 0)
                $('#searchTask').val("Выбрано: " + WGM.NumberOfSelectedTasks());
            else
                $('#searchTask').val(text);

            //Select TypeWork
            var a1 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[4].children[0].children[0];
            $(a1).html(text);
            var kind = "";
            if (_kindWorksOut.length > 0) {
                for (var i = 0; i < _kindWorksOut.length; i++) {
                    kind += _kindWorksOut[i];
                    if (i < _kindWorksOut.length - 1)
                        kind += "; ";
                }
                $(a1).html(kind);
            }

            //Select BasePlan
            var a2 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[0].children[0].children[0];
            $(a2).html(text);
            if (_targetBasePlan != undefined && _targetBasePlan != null && _targetBasePlan != "") {
                var textBase = "Базовый план";
                var targetBasePlan = _targetBasePlan * 1;
                if (targetBasePlan > 0)
                    textBase += " " + targetBasePlan;
                $(a2).html(textBase);
            }
            //Select Floor
            var a3 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[2].children[0].children[0];
            $(a3).html(text);
            var floorT = "";
            if (_floorOut.length > 0) {
                for (var i = 0; i < _floorOut.length; i++) {
                    floorT += _floorOut[i];
                    if (i < _floorOut.length - 1)
                        floorT += "; ";
                }
                $(a3).html(floorT);
            }

            //Select Blok
            var a4 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[4].children[0].children[0];
            $(a4).html(text);
            var bloksT = "";
            if (_bloksOut.length > 0) {
                for (var i = 0; i < _bloksOut.length; i++) {
                    bloksT += _bloksOut[i];
                    if (i < _bloksOut.length - 1)
                        bloksT += "; ";
                }
                $(a4).html(bloksT);
            }

            //Select Blok2
            var a41 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[0].children[0].children[0];
            $(a41).html(text);
            var bloksT = "";
            if (_bloks2Out.length > 0) {
                for (var i = 0; i < _bloks2Out.length; i++) {
                    bloksT += _bloks2Out[i];
                    if (i < _bloks2Out.length - 1)
                        bloksT += "; ";
                }
                $(a41).html(bloksT);
            }

            //Select Capture
            var a42 = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[2].children[0].children[0];
            $(a42).html(text);
            var bloksT = "";
            if (_captureOut.length > 0) {
                for (var i = 0; i < _captureOut.length; i++) {
                    bloksT += _captureOut[i];
                    if (i < _captureOut.length - 1)
                        bloksT += "; ";
                }
                $(a42).html(bloksT);
            }

            //Select ViewPerformance
            var a5 = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[0].children[0].children[0];
            $(a5).html(text);
            if (_targetViewName != undefined && _targetViewName != "")
                $(a5).html(_targetViewName);

            //Select TimeScale
            var a6 = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[2].children[0].children[0];
            text = WGM.NumTimeScale(_targetTimeScale);
            $(a6).html(text);

            //Select SchedulePeriod
            var a7 = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[4].children[0].children[0];
            text = WGM.NumSchedulePeriod(_schedulePeriod);
            $(a7).html(text);

        }
        catch (e) {
            console.log(e);
        }

    }





};

function RegisterCustomDateTimePropType() {

    // Derive from the String property type.
    var basePropType = SP.JsGrid.Internal.Property.GetPropType('JSDateTime');
    var CustomDateTimePropType = SP.Internal.JS.object(basePropType);

    CustomDateTimePropType.ID = 'CustomDateTime';

    // Define the validation logic. This example uses a regular expression
    // to specify that only letters and spaces are allowed.
    CustomDateTimePropType.BeginValidateNormalizeConvert = function (
        recordKey, fieldKey, newValue, bIsLocalized, fnCallback, fnError) {
        basePropType.BeginValidateNormalizeConvert.apply(this, [recordKey,
            fieldKey, newValue, bIsLocalized, PostProcess, fnError]);
        function PostProcess(result) {
            result.isValid = result.isValid;
            result.dataValue = result.normalizedLocValue = newValue == '' ? null : newValue;

            var ds = _jsGridControl.GetRecord(recordKey).properties["Start"].localizedValue.split(".");
            var oldDs = ds;
            var df = _jsGridControl.GetRecord(recordKey).properties["Finish"].localizedValue.split(".");
            var oldDf = df;
            if (fieldKey == "Start")
                ds = newValue.split(".");
            if (fieldKey == "Finish")
                df = newValue.split(".");

            dateStart = (new Date(+ds[2], +ds[1] - 1, +ds[0]));
            dateFinish = (new Date(+df[2], +df[1] - 1, +df[0]));

            if (dateStart > dateFinish) {
                if (fieldKey == "Start") {
                    var d = (dateStart - (new Date(+oldDs[2], +oldDs[1] - 1, +oldDs[0]))) / (1000 * 60 * 60 * 24);
                    dateFinish.setDate(dateFinish.getDate() + d);
                    var update = SP.JsGrid.CreateUnvalidatedPropertyUpdate(recordKey, "Finish", dateFinish.toLocaleDateString(), false);
                    _jsGridControl.UpdateProperties([update], SP.JsGrid.UserAction.UserEdit);
                }
                if (fieldKey == "Finish") {
                    var d = ((new Date(+oldDf[2], +oldDf[1] - 1, +oldDf[0])) - dateFinish) / (1000 * 60 * 60 * 24);
                    dateStart.setDate(dateStart.getDate() - d);
                    var update = SP.JsGrid.CreateUnvalidatedPropertyUpdate(recordKey, "Start", dateStart.toLocaleDateString(), false);
                    _jsGridControl.UpdateProperties([update], SP.JsGrid.UserAction.UserEdit);
                }
            }

            result.isValid = true;
            fnCallback(result);
        }
    };
    SP.JsGrid.PropertyType.RegisterNewDerivedCustomPropType(CustomDateTimePropType, 'JSDateTime');
}

function CreatePsGridFieldsToolTip(basePropType) {
    var newPropType = SP.Internal.JS.object(basePropType);
    newPropType.ID = 'PsGridFieldsToolTip';
    newPropType.BeginValidateNormalizeConvert = function (recordKey, fieldKey, newValue, bIsLocalized, fnCallback, fnError) {
        if (!newValue) {
            fnCallback({ isValid: false, dataValue: newValue, normalizedLocValue: newValue, errorMsg: 'Value is not set.' });
        } else {
            $.ajax({
                url: 'some_validation_url',
                data: {
                    value: newValue
                },
                success: function (result) {
                    fnCallback(result);
                },
                error: function (request) {
                    fnCallback({ isValid: false, dataValue: newValue, normalizedLocValue: newValue, errorMsg: request.responseText });
                }
            });
        }
    };
    newPropType.widgetControlNames = ['PsGridFieldsToolTipWidget'];
    newPropType.DisplayControlNames = ['PsGridFieldsToolTipDisplay'];
    return newPropType;
}



function CreatePsGridFieldsToolTipDisplay() {
    SP.JsGrid.PropertyType.Utils.RegisterDisplayControl(
        'PsGridFieldsToolTipDisplay',
        {
            Id: "PsGridFieldsToolTipDisplay",
            Render: function (value, record, column, field, propType, style, jsGridObj, RTL, containerElem) {
                return jsGridObj.IsGroupingRecordKey(record.key()) ? null : RenderPsGridFieldsToolTip(false, value.data.htmlText, null);
            },
            FocusNext: function (domElementsCollection, focusedDomElement) {
                return SP.JsGrid.Utility.FocusElementInDOMCollection(domElementsCollection, focusedDomElement, true);
            },
            FocusPrevious: function (domElementsCollection, focusedDomElement) {
                return SP.JsGrid.Utility.FocusElementInDOMCollection(domElementsCollection, focusedDomElement, false);
            }
        },
        []);
}

function CreatePsGridFieldsToolTipWidget() {
    SP.JsGrid.PropertyType.Utils.RegisterWidgetControl(
        'PsGridFieldsToolTipWidget',
        function (gridContext) {
            return new MyCustomWidget(gridContext);
        },
        []);
    MyCustomWidget = function (gridContext) {
        this.SupportedWriteMode = SP.JsGrid.EditActorWriteType.DataOnly;
        this.SupportedReadMode = SP.JsGrid.EditActorReadType.DataOnly;
        var self = this;
        this.cellContext = null;

        this.Dispose = function () {
        };
        this.GetIcon = function () {
            return new SP.JsGrid.Image('../images/editheader.png').Render('');
        };
        this.OnValueChanged = function () {
        };
        this.Expand = function () {
            var options = {
                allowautoresize: false,
                allowmaximize: false,
                title: 'Pick Resource',
                width: 600,
                height: 400
            };

            SP.UI.ModalDialog.showModalDialog({
                url: 'some_url',
                args: {},
                dialogReturnValueCallback: function (oDialogResult, oRet) {
                    if (oDialogResult) {
                        self.cellContext.SetCurrentValue({ data: oRet.Guid, localized: oRet.Name });
                        self.cellContext.NotifyEditComplete();
                    }
                    self.Collapse();
                }
            });
        };
        this.Collapse = function () {
            this.cellContext.NotifyCollapseWidget();
        };
        this.BindToCell = function (cellContext) {
            this.cellContext = cellContext;
        };
        this.Unbind = function () {
        };
    };
}

function RenderPsGridFieldsToolTip(bDisabled, dataValue, fnOnCheckChanged) {
    var r = document.createElement('div');

    r.style.cssText = 'text-align:center';
    r.innerHTML = dataValue;

    return r;
}





function MenuTaskReportTasks() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[0].children[0].children[0].children[1].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[0].children[0].children[0].children[1];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu ms-cui-menu32" id="Ri.TaskReport.Tasks.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 157px; left: 0px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 60px;"><div unselectable="on" class="ms-cui-smenu-inner"><div unselectable="on" class="" id="Ri.TaskReport.Tasks.Menu.SubmitSection"><div unselectable="on" class="ms-cui-menusection"><ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items32"><li unselectable="on" class="ms-cui-menusection-items">';
        newMenu += '<a unselectable="on" href="javascript:;" onclick="WGM.SendAllApproval(); return false;" class="ms-cui-ctl-menu ms-cui-ctl-menu32 " mscui:controltype="Button" role="button" id="Ri.TaskReport.Tasks.Menu.SubmitSection.SendApproval">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-iconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -256px; left: -160px;"></span></span><span unselectable="on" class="ms-cui-ctl-menulabel"><span unselectable="on" class="ms-cui-ctl-mediumlabel ms-cui-btn-title">Все задачи</span><span unselectable="on" class="ms-cui-btn-menu-description" style="display: block;">Сохранение и отправка всех обновлений состояния задач для утверждения руководителем.</span></span><span unselectable="on" class="ms-cui-ctl-menu32clear">&nbsp;</span><span unselectable="on" class="ms-cui-glass-ff"></span></a></li><li unselectable="on" class="ms-cui-menusection-items">';
        newMenu += '<a unselectable="on" href="javascript:;" onclick="WGM.SendApproval(); return false;" class="ms-cui-ctl-menu ms-cui-ctl-menu32 " mscui:controltype="Button" role="button" id="Ri.TaskReport.Tasks.Menu.SubmitSection.SendAllApproval"><span unselectable="on" class="ms-cui-ctl-iconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -288px; left: -96px;"></span></span><span unselectable="on" class="ms-cui-ctl-menulabel"><span unselectable="on" class="ms-cui-ctl-mediumlabel ms-cui-btn-title">Выбранные задачи</span><span unselectable="on" class="ms-cui-btn-menu-description" style="display: block;">Сохранение и отправка обновлений состояния выбранной задачи для утверждения руководителем.</span></span>';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-menu32clear">&nbsp;</span><span unselectable="on" class="ms-cui-glass-ff"></span></a></li></ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[0].children[0].children[0].children[1];
        $(span).html(newMenu);
    }

}

function CloseAllMenu() {
    try {

        //MenuTaskReportTasks
        if ($('.ms-cui-tabContainer')[0].children[0].children[0].children[0].children[0].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[0].children[0].children[0].children[1];
            $(menu).html("");
        }
        //MenuActiveProject
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
            $(menu).html("");
        }
        //MenuActiveTask
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[3].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[3];
            $(menu).html("");

            if (_filterTasksOut.length > 0)
                $('#searchTask').val("Выбрано: " + WGM.NumberOfSelectedTasks());
            else
                $('#searchTask').val("Выбрать ... ");


        }
        //MenuActiveTypeWork
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[5].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[5];
            $(menu).html("");
        }
        //MenuActiveBasePlan
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[1];
            $(menu).html("");
        }
        //MenuActiveFloor
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[3].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[3];
            $(menu).html("");
        }
        //MenuActiveBlok
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[5].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[5];
            $(menu).html("");
        }
        //MenuActiveBlok2
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[1];
            $(menu).html("");
        }
        //MenuActiveCapture
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[3].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[3];
            $(menu).html("");
        }
        //RiTaskReportViewLayout() {
        if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[2].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[2].children[1];
            $(menu).html("");
        }
        //RiTaskReportScale
        if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[3].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[3].children[1];
            $(menu).html("");
        }
        //FilterSelectProject(uidProject)
        if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
            $(menu).html("");
        }
        //MenuViewPerformance
        if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[1].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
            $(menu).html("");
        }
        //MenuTaskReportViewTimeScale
        if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[3].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[3];
            $(menu).html("");
        }
        //MenuaskReportViewSchedulePeriod
        if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[5].children[0] != null) {
            var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[5];
            $(menu).html("");
        }
    }
    catch (e) {
        console.log(e);
    }
}



function MenuActiveProject() {
    //  $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1]
    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.Project.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 112px; left: 134px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;">' +
            '<div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.Project.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul style="margin-left: 0; padding-left: 0; text-align: left;">';

        for (var i = 0; i < _projects.length; i++) {
            var proj = _projects[i];
            newMenu += '<li style="list-style-type: none;">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.FilterSelectProject(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + proj.Id + '">' +
                '<span unselectable="on" class="ms-cui-ctl-mediumlabel">' + proj.Name + '</span>' +
                '</a>' +
                '</li>';
        }

        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
        $(span).html(newMenu);
    }
}

function MenuActiveTask() {

    var val = $('#searchTask').val();
    if (val == 'Выбрать ... ' || (val.toLowerCase().indexOf("Выбрано: ".toLowerCase())) > -1) {
        $('#searchTask').val("");
        val = "";
    }

    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[3].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[3];
        $(menu).html("");

        if (val == "" || val == "")
            $('#searchTask').val("Выбрать ... ");
        if (_filterTasksOut.length > 0)
            $('#searchTask').val("Выбрано: " + _filterTasksOut.length);
    }
    else {//style="padding-left: 15px;"
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.Task.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 136px; left: 134px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.Task.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items16">';

        var derivedTasks = [];
        for (var i = 0; i < _filterTasks.length; i++) {
            var task = _filterTasks[i];
            if ((val == "" || val == " " || (task.Name.toLowerCase()).indexOf(val.toLowerCase()) > -1) && derivedTasks.indexOf(task.Name) == -1) {
                newMenu += '<li unselectable="on" class="ms-cui-menusection-items">' +
                    '<a unselectable="on" href="javascript:;" onclick="WGM.FilterSelectTask(this); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + task.Id + '" name="' + task.Name + '">' +
                    '<span unselectable="on" class="ms-cui-ctl-iconContainer" style="margin: 0px -3px;"><span unselectable="on" style="width: 20px !important;" class=" ms-cui-img-16by16 ms-cui-img-cont-float">' +
                    '<input type="checkbox" tabindex="-1" ' + (_filterTasksOut.indexOf(task.Id) == -1 ? '' : 'checked') + '></span></span>' +
                    '<span unselectable="on" class="ms-cui-ctl-mediumlabel" style="padding-left: 15px;">' + task.Name + '</span><span unselectable="on" class="ms-cui-glass-ff"></span>' +
                    '</a>' +
                    '</li>';
                derivedTasks.push(task.Name);
            }
        }

        newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.FilterSelectTask(); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
        newMenu += '<span unselectable="on" style="vertical-align: middle; float: left;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
        newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
        newMenu += '</a></li>';

        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[3];
        $(span).html(newMenu);
    }
}

function MenuActiveTaskInput() {
    var val = $('#searchTask').val();
    if (val == 'Выбрать ... ' || (val.toLowerCase().indexOf("Выбрано: ".toLowerCase())) > -1) {
        $('#searchTask').val("");
        val = "";
    }

    //CloseAllMenu();
    var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.Task.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 136px; left: 134px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
        '<div unselectable="on" class="" id="Ri.TaskReport.Data.Task.List.Menu.InboxFilters">' +
        '<div unselectable="on" class="custom-menu-container">' +
        '<ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items16">';

    var derivedTasks = [];
    for (var i = 0; i < _filterTasks.length; i++) {
        var task = _filterTasks[i];
        if ((val == "" || val == " " || (task.Name.toLowerCase()).indexOf(val.toLowerCase()) > -1) && derivedTasks.indexOf(task.Name) == -1) {
            newMenu += '<li unselectable="on" class="ms-cui-menusection-items">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.FilterSelectTask(this); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + task.Id + '"  name="' + task.Name + '">' +
                '<span unselectable="on" class="ms-cui-ctl-iconContainer" style="margin: 0px -3px;"><span unselectable="on" style="width: 20px !important;" class=" ms-cui-img-16by16 ms-cui-img-cont-float">' +
                '<input type="checkbox" tabindex="-1" ' + (_filterTasksOut.indexOf(task.Id) == -1 ? '' : 'checked') + '></span></span>' +
                '<span unselectable="on" class="ms-cui-ctl-mediumlabel" style="padding-left: 15px;">' + task.Name + '</span><span unselectable="on" class="ms-cui-glass-ff"></span>' +
                '</a>' +
                '</li>';
            derivedTasks.push(task.Name);
        }
    }

    newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.FilterSelectTask(); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
    newMenu += '<span unselectable="on" style="vertical-align: middle; float: left;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
    newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
    newMenu += '</a></li>';

    newMenu += '</ul></div></div></div></div>';
    var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[3];
    $(span).html(newMenu);

}

function MenuActiveTypeWork() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[5].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[5];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.TypeWork.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 160px; left: 134px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.TypeWork.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items16">';
        for (key in _kindWorks) {
            newMenu += '<li unselectable="on" class="ms-cui-menusection-items">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.RunDataTypeWork(this); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + key + '">' +
                '<span unselectable="on" class="ms-cui-ctl-iconContainer" style="margin: 0px -3px;"><span unselectable="on" style="width: 20px !important;" class=" ms-cui-img-16by16 ms-cui-img-cont-float">' +
                '<input type="checkbox" tabindex="-1" ' + (_kindWorksOut.indexOf(key) == -1 ? '' : 'checked') + '></span></span>' +
                '<span unselectable="on" class="ms-cui-ctl-mediumlabel" style="padding-left: 15px;">' + key + ' ' + _kindWorks[key] + '</span><span unselectable="on" class="ms-cui-glass-ff"></span>' +
                '</a>' +
                '</li>';
        }

        newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.RunDataTypeWork(); return false;"" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
        newMenu += '<span unselectable="on" style="float: left; vertical-align: middle;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
        newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
        newMenu += '</a></li>';


        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[1].children[5];
        $(span).html(newMenu);
    }
}

function MenuActiveBasePlan() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[1].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[1];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.BasePlan.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 112px; left: 349.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;">' +
            '<div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.BasePlan.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="ms-cui-menusection">' +
            '<ul style="margin-left: 0; padding-left: 0; text-align: left;">';

        for (var i = 0; i < _basePlans.length; i++) {
            newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunDataBasePlan(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + _basePlans[i] + '">';
            newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Базовый план';
            if (_basePlans[i] > 0)
                newMenu += ' ' + _basePlans[i];
            newMenu += '</span></a></li>';
        }

        newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.RunDataBasePlan(); return false;"" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
        newMenu += '<span unselectable="on" style="vertical-align: middle;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
        newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
        newMenu += '</a></li>';

        newMenu += '</ul></div></div>';

        newMenu += '</div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[1];
        $(span).html(newMenu);
    }
}


function MenuActiveFloor() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[3].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[3];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.Floor.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 136px; left: 349.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.Floor.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items16">';
        for (var i = 0; i < _floor.length; i++) {
            newMenu += '<li unselectable="on" class="ms-cui-menusection-items">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.RunDataFloor(this); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + _floor[i] + '">' +
                '<span unselectable="on" class="ms-cui-ctl-iconContainer" style="margin: 0px -3px;"><span unselectable="on" style="width: 20px !important;" class=" ms-cui-img-16by16 ms-cui-img-cont-float">' +
                '<input type="checkbox" tabindex="-1" ' + (_floorOut.indexOf(_floor[i]) == -1 ? '' : 'checked') + '></span></span>' +
                '<span unselectable="on" class="ms-cui-ctl-mediumlabel" style="padding-left: 15px;">' + _floor[i] + '</span><span unselectable="on" class="ms-cui-glass-ff"></span>' +
                '</a>' +
                '</li>';
        }

        newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.RunDataFloor(); return false;"" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
        newMenu += '<span unselectable="on" style="float: left; vertical-align: middle;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
        newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
        newMenu += '</a></li>';

        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[3];
        $(span).html(newMenu);
    }
}

function MenuActiveBlok() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[5].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[5];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.Blok.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 160px; left: 349.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.Blok.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items16">';
        for (var i = 0; i < _blok.length; i++) {
            newMenu += '<li unselectable="on" class="ms-cui-menusection-items">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.RunDataBlok(this); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + _blok[i] + '">' +
                '<span unselectable="on" class="ms-cui-ctl-iconContainer" style="margin: 0px -3px;"><span unselectable="on" style="width: 20px !important;" class=" ms-cui-img-16by16 ms-cui-img-cont-float">' +
                '<input type="checkbox" tabindex="-1" ' + (_bloksOut.indexOf(_blok[i]) == -1 ? '' : 'checked') + '></span></span>' +
                '<span unselectable="on" class="ms-cui-ctl-mediumlabel" style="padding-left: 15px;">' + _blok[i] + '</span><span unselectable="on" class="ms-cui-glass-ff"></span>' +
                '</a>' +
                '</li>';
        }

        newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.RunDataBlok(); return false;"" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
        newMenu += '<span unselectable="on" style="float: left; vertical-align: middle;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
        newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
        newMenu += '</a></li>';


        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[3].children[5];
        $(span).html(newMenu);
    }
}

function MenuActiveBlok2() {
    console.log('Menu blok2 enter');
    // $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[1]
    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[1].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[1];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.Blok2.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 112px; left: 540.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.Blok2.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items16">';
        for (var i = 0; i < _blok2.length; i++) {
            newMenu += '<li unselectable="on" class="ms-cui-menusection-items">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.RunDataBlok2(this); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + _blok2[i] + '">' +
                '<span unselectable="on" class="ms-cui-ctl-iconContainer" style="margin: 0px -3px;"><span unselectable="on" style="width: 20px !important;" class=" ms-cui-img-16by16 ms-cui-img-cont-float">' +
                '<input type="checkbox" tabindex="-1" ' + (_bloks2Out.indexOf(_blok2[i]) == -1 ? '' : 'checked') + '></span></span>' +
                '<span unselectable="on" class="ms-cui-ctl-mediumlabel" style="padding-left: 15px;">' + _blok2[i] + '</span><span unselectable="on" class="ms-cui-glass-ff"></span>' +
                '</a>' +
                '</li>';
        }

        newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.RunDataBlok2(); return false;"" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
        newMenu += '<span unselectable="on" style="float: left; vertical-align: middle;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
        newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
        newMenu += '</a></li>';


        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[1];
        $(span).html(newMenu);
    }
}

function MenuActiveCapture() {
    console.log('Menu Capture enter');
    if ($('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[3].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[3];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        console.log(_capture);
        console.log(_captureOut);
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.Data.Capture.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 136px; left: 540.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.Data.Capture.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items16">';
        for (var i = 0; i < _capture.length; i++) {
            newMenu += '<li unselectable="on" class="ms-cui-menusection-items">' +
                '<a unselectable="on" href="javascript:;" onclick="WGM.RunDataCapture(this); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + _capture[i] + '">' +
                '<span unselectable="on" class="ms-cui-ctl-iconContainer" style="margin: 0px -3px;"><span unselectable="on" style="width: 20px !important;" class=" ms-cui-img-16by16 ms-cui-img-cont-float">' +
                '<input type="checkbox" tabindex="-1" ' + (_captureOut.indexOf(_capture[i]) == -1 ? '' : 'checked') + '></span></span>' +
                '<span unselectable="on" class="ms-cui-ctl-mediumlabel" style="padding-left: 15px;">' + _capture[i] + '</span><span unselectable="on" class="ms-cui-glass-ff"></span>' +
                '</a>' +
                '</li>';
        }

        newMenu += '<li style="list-style-type: none; border-top: 1px solid rgb(198, 198, 198);"><a unselectable="on" href="javascript:;" onclick="WGM.RunDataCapture(); return false;"" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="">';
        newMenu += '<span unselectable="on" style="float: left; vertical-align: middle;"><span unselectable="on" class=" ms-cui-img-16by16 ms-cui-img-cont-float"><img unselectable="on" alt="Настраиваемый фильтр..." src="/_layouts/15/1049/images/ps16x16.png?rev=43" style="top: -52px; left: -32px;"></span></span>';
        newMenu += '<span unselectable="on" style="color: #23272c; padding: 3px 3px;">Нет фильтра</span>';
        newMenu += '</a></li>';


        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[1].children[0].children[0].children[0].children[0].children[0].children[5].children[3];
        $(span).html(newMenu);
    }
}

function MenuViewPerformance() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[1].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.View.Performance.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 112px; left: 914.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.View.Performance.List.Menu.InboxFilters">' +
            '<div unselectable="on" class="custom-menu-container">' +
            '<ul style="margin-left: 0; padding-left: 0; text-align: left;">';
        for (var i = 0; i < _representation.length; i++) {
            newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewPerformance(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="' + _representation[i] + '">';
            newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel" >' + _representation[i] + '</span>';
            newMenu += '</a></li>';
        }
        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[1];
        $(span).html(newMenu);
    }
}


function RiTaskReportViewLayout() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[2].children[1].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[2].children[1];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu ms-cui-menu32" id="Ri.TaskReport.View.Layout.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 157px; left: 960px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 40px;"><div unselectable="on" class="ms-cui-smenu-inner"><div unselectable="on" class="" id="Ribbon.ContextualTabs.MyWork.Home.Navigate.Display.Menu.Displays"><div unselectable="on" class="ms-cui-menusection"><ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items32">' +
            '<li unselectable="on" class="ms-cui-menusection-items"><a unselectable="on" href="javascript:;" onclick="WGM.ViewLayoutMenu(1); return false;" class="ms-cui-ctl-menu ms-cui-ctl-menu32 " mscui:controltype="ToggleButton" role="button" id="Ri.TaskReport.View.Layout.Menu.DisplayGantt" aria-pressed="false"><span unselectable="on" class="ms-cui-ctl-iconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="Диаграмма Ганта" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -352px; left: -320px;"></span></span><span unselectable="on" class="ms-cui-ctl-menulabel"><span unselectable="on" class="ms-cui-ctl-mediumlabel ms-cui-btn-title">Диаграмма Ганта</span><span unselectable="on" class="ms-cui-btn-menu-description" style="display: block;">Отображение или скрытие диаграммы Ганта.</span></span><span unselectable="on" class="ms-cui-ctl-menu32clear">&nbsp;</span><span unselectable="on" class="ms-cui-glass-ff"></span></a></li>' +
            '<li unselectable="on" class="ms-cui-menusection-items"><a unselectable="on" href="javascript:;" onclick="WGM.ViewLayoutMenu(2); return false;" class="ms-cui-ctl-menu ms-cui-ctl-menu32 " mscui:controltype="ToggleButton" role="button" id="Ri.TaskReport.View.Layout.Menu.DisplayTimephased" aria-pressed="true"><span unselectable="on" class="ms-cui-ctl-iconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="Повременные данные" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -256px; left: -288px;"></span></span><span unselectable="on" class="ms-cui-ctl-menulabel"><span unselectable="on" class="ms-cui-ctl-mediumlabel ms-cui-btn-title">Повременные данные</span><span unselectable="on" class="ms-cui-btn-menu-description" style="display: block;">Отображение данных в повременном представлении.</span></span><span unselectable="on" class="ms-cui-ctl-menu32clear">&nbsp;</span><span unselectable="on" class="ms-cui-glass-ff"></span></a></li>' +
            '<li unselectable="on" class="ms-cui-menusection-items"><a unselectable="on" href="javascript:;" onclick="WGM.ViewLayoutMenu(0); return false;" class="ms-cui-ctl-menu ms-cui-ctl-menu32 " mscui:controltype="ToggleButton" role="button" id="Ri.TaskReport.View.Layout.Menu.DisplayNone" aria-pressed="false"><span unselectable="on" class="ms-cui-ctl-iconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="Лист" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -288px; left: -32px;"></span></span><span unselectable="on" class="ms-cui-ctl-menulabel"><span unselectable="on" class="ms-cui-ctl-mediumlabel ms-cui-btn-title">Лист</span><span unselectable="on" class="ms-cui-btn-menu-description" style="display: block;">Отображение данных в формате таблицы.</span></span><span unselectable="on" class="ms-cui-ctl-menu32clear">&nbsp;</span><span unselectable="on" class="ms-cui-glass-ff"></span></a></li>' +
            '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[2].children[1];
        $(span).html(newMenu);
    }
};

function RiTaskReportScale() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[3].children[1].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[3].children[1];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu ms-cui-menu32" id="Ri.TaskReport.View.Scale.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 157px; left: 995px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 40px;"><div unselectable="on" class="ms-cui-smenu-inner"><div unselectable="on" class="" id="Ribbon.ContextualTabs.MyWork.Home.Navigate.Display.Menu.Displays"><div unselectable="on" class="ms-cui-menusection"><ul unselectable="on" class="ms-cui-menusection-items ms-cui-menusection-items32">' +
            '<li unselectable="on" class="ms-cui-menusection-items"><a unselectable="on" href="javascript:;" onclick="WGM.ViewScaleMenu(0); return false;" class="ms-cui-ctl-menu ms-cui-ctl-menu32 " mscui:controltype="ToggleButton" role="button" id="Ri.TaskReport.View.Scale.Menu.Default" aria-pressed="false"><span unselectable="on" class="ms-cui-ctl-iconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="По умолчанию" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -96px; left: -32px;"></span></span><span unselectable="on" class="ms-cui-ctl-menulabel"><span unselectable="on" class="ms-cui-ctl-mediumlabel ms-cui-btn-title">По умолчанию</span><span unselectable="on" class="ms-cui-btn-menu-description" style="display: block;">Масштаб ширина: 100%, высота: 600px.</span></span><span unselectable="on" class="ms-cui-ctl-menu32clear">&nbsp;</span><span unselectable="on" class="ms-cui-glass-ff"></span></a></li>' +
            '<li unselectable="on" class="ms-cui-menusection-items"><a unselectable="on" href="javascript:;" onclick="WGM.ViewScaleMenu(1); return false;" class="ms-cui-ctl-menu ms-cui-ctl-menu32 " mscui:controltype="ToggleButton" role="button" id="Ri.TaskReport.View.Scale.Menu.Alternative" aria-pressed="true"><span unselectable="on" class="ms-cui-ctl-iconContainer"><span unselectable="on" class=" ms-cui-img-32by32 ms-cui-img-cont-float"><img unselectable="on" alt="Альтернативный" src="/_layouts/15/1049/images/ps32x32.png?rev=43" style="top: -96px; left: -32px;"></span></span><span unselectable="on" class="ms-cui-ctl-menulabel"><span unselectable="on" class="ms-cui-ctl-mediumlabel ms-cui-btn-title">Альтернативный</span><span unselectable="on" class="ms-cui-btn-menu-description" style="display: block;">Масштаб ширина: 100%, высота: 800px.</span></span><span unselectable="on" class="ms-cui-ctl-menu32clear">&nbsp;</span><span unselectable="on" class="ms-cui-glass-ff"></span></a></li>' +
            '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[3].children[1];
        $(span).html(newMenu);
    }
};

function MenuTaskReportViewTimeScale() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[3].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[3];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.View.TimeScale.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 136px; left: 914.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.View.TimeScale.List.Menu.InboxFilters"><div unselectable="on" class="ms-cui-menusection">' +
            '<ul style="margin-left: 0; padding-left: 0; text-align: left;">';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewTimeScale(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="0">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">День</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewTimeScale(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="1">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Неделя</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewTimeScale(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="2">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Месяц</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewTimeScale(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="3">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Квартал</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewTimeScale(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="4">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Год</span>';
        newMenu += '</a></li>';

        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[3];
        $(span).html(newMenu);
    }
};

function MenuaskReportViewSchedulePeriod() {
    if ($('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[5].children[0] != null) {
        var menu = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[5];
        $(menu).html("");
    }
    else {
        CloseAllMenu();
        var newMenu = '<div unselectable="on" class="ms-cui-menu" id="Ri.TaskReport.View.SchedulePeriod.List.Menu" role="menu" style="direction: ltr; visibility: visible; position: fixed; top: 160px; left: 914.5px; z-index: 1001; max-height: none; overflow-y: auto; width: auto; min-width: 127px;"><div unselectable="on" class="ms-cui-smenu-inner">' +
            '<div unselectable="on" class="" id="Ri.TaskReport.View.SchedulePeriod.List.Menu.InboxFilters"><div unselectable="on" class="ms-cui-menusection">' +
            '<ul style="margin-left: 0; padding-left: 0; text-align: left;">';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewSchedulePeriod(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="0">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Текущая неделя</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewSchedulePeriod(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="1">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Текущий месяц</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewSchedulePeriod(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="2">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Текущий квартал</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewSchedulePeriod(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="3">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Выбрать период</span>';
        newMenu += '</a></li>';

        newMenu += '<li style="list-style-type: none;"><a unselectable="on" href="javascript:;" onclick="WGM.RunViewSchedulePeriod(this.id); return false;" class="ms-cui-ctl-menu " mscui:controltype="Button" role="button" id="4">';
        newMenu += '<span unselectable="on" class="ms-cui-ctl-mediumlabel">Весь период</span>';
        newMenu += '</a></li>';

        newMenu += '</ul></div></div></div></div>';
        var span = $('.ms-cui-tabContainer')[0].children[0].children[2].children[0].children[0].children[0].children[0].children[0].children[1].children[5];
        $(span).html(newMenu);
    }
}

function CheckMenu() {
    try {
        var RiTaskReportTasksMenu = document.getElementById('Ri.TaskReport.Tasks.Menu');
        var RiTaskReportDataProjectListMenu = document.getElementById('Ri.TaskReport.Data.Project.List.Menu');
        var RiTaskReportDataTaskListMenu = document.getElementById('Ri.TaskReport.Data.Task.List.Menu');
        var RiTaskReportDataTypeWorkListMenu = document.getElementById('Ri.TaskReport.Data.TypeWork.List.Menu');
        var RiTaskReportDataBasePlanListMenu = document.getElementById('Ri.TaskReport.Data.BasePlan.List.Menu');
        var RiTaskReportDataFloorListMenu = document.getElementById('Ri.TaskReport.Data.Floor.List.Menu');
        var RiTaskReportDataBlokListMenu = document.getElementById('Ri.TaskReport.Data.Blok.List.Menu');
        var RiTaskReportViewPerformanceListMenu = document.getElementById('Ri.TaskReport.View.Performance.List.Menu');
        var RiTaskReportViewLayoutMenu = document.getElementById('Ri.TaskReport.View.Layout.Menu');
        var RiTaskReportScaleMenu = document.getElementById('Ri.TaskReport.View.Scale.Menu');
        var RiTaskReportViewTimeScaleListMenu = document.getElementById('Ri.TaskReport.View.TimeScale.List.Menu');
        var RiTaskReportViewSchedulePeriodListMenu = document.getElementById('Ri.TaskReport.View.SchedulePeriod.List.Menu');

        if (RiTaskReportTasksMenu != null ||
            RiTaskReportDataProjectListMenu != null ||
            RiTaskReportDataTaskListMenu != null ||
            RiTaskReportDataTypeWorkListMenu != null ||
            RiTaskReportDataBasePlanListMenu != null ||
            RiTaskReportDataFloorListMenu != null ||
            RiTaskReportDataBlokListMenu != null ||
            RiTaskReportViewPerformanceListMenu != null ||
            RiTaskReportViewLayoutMenu != null ||
            RiTaskReportScaleMenu != null ||
            RiTaskReportViewTimeScaleListMenu != null ||
            RiTaskReportViewSchedulePeriodListMenu != null)
            CloseAllMenu();
    }
    catch (ex) {
        console.log(ex);
    }

}

$("#s4-workspace, .ms-cui-ribbonTopBars, #suiteBarDelta").mouseup(function () {
    try {

        CheckMenu();
    }
    catch (ex) {
        console.log(ex);
    }
});


