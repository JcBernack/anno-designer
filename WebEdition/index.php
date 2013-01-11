<? error_reporting(E_ALL); ?>
<?
//require("config.php");
//$db = @new mysqli($db_address, $db_username, $db_password, $db_database);
//$layout = $db->query("select * from layout")->fetch_assoc();
//$db->close();
?>
<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8">
    <title>Anno Designer - Web Edition</title>
    <script src="lib/jquery-1.8.3.min.js"></script>
    <script src="lib/jquery-ui-1.9.2.min.js"></script>
    <script src="lib/jquery.dataTables.min.js"></script>
    <script src="lib/jquery.themeswitcher.min.js"></script>
    <script src="lib/jquery.miniColors.js"></script>
    <script src="lib/json2.js"></script>
    <script src="helpers.js"></script>
    <script src="designer.js"></script>
    <link href="styles/jquery.miniColors.css" rel="stylesheet" type="text/css">
    <link href="styles/pepper-grinder/jquery-ui-1.9.2.custom.min.css" rel="stylesheet" type="text/css">
    <link href="styles/jquery.dataTables_themeroller.css" rel="stylesheet" type="text/css">
    <link href="styles/styles.css" rel="stylesheet" type="text/css">
    <script type="text/javascript" language="JavaScript">
        var designer;
        $(function () {
            // initialize theme switcher
            // https://github.com/harborhoffer/Super-Theme-Switcher
            $("#themeSwitcher").themeswitcher({
                loadtheme: "pepper-grinder",
                imgpath: "styles/themes/",
                width: 250,
                height: 500,
                closeOnSelect: false
            });
            // initialize menu buttons
            $("#login").button({ icons: { primary: "ui-icon-key" } });
            $("#register").button({ icons: { primary: "ui-icon-person" } });
            $("#toggleEditor").button({ icons: { primary: "ui-icon-arrow-4-diag" } });
            $("#toggleEditor").click(function(e) {
                // instant
                $("#preview").toggleClass("preview");
                $("#list").toggle();
                // animated
//                var effect = {
//                    effect: "fade",
//                    duration: 500
//                };
//                $("#preview").toggleClass("preview", 500);
//                $(this).button({ disabled: true });
//                $("#list").hide({
//                    effect: "fade",
//                    duration: 500,
//                    complete: function() {
//                        $("#preview").toggleClass("preview", 500, function() {
//                            $(this).button({ disabled: false });
//                        });
//                    }
//                });
            });
            // initialize layout rendering
            designer = new Designer({
            });
            //designer.Render();
            // initialize datatable
            var datatable = $("#listTable").dataTable({
                bJQueryUI:true,
                bProcessing:true,
                sAjaxSource:"rest/layout",
                aoColumns:[
                    { mData:"name" },
                    { mData:"author" },
                    { mData:"width" },
                    { mData:"height" },
                    { mData:"created" },
                    { mData:"edited" }
                ],
                aaSorting: [[4, 'desc', 0]],
                fnInitComplete:function (oSettings, json) {
                    if (json.aaData.length > 0) {
                        $("#" + json.aaData[0].ID).click();
                    }
                }
            });
            // add a click handler to the rows
            $("#listTable tbody tr").live("click", function (event) {
                if (!$(this).hasClass('row_selected')) {
                    datatable.$('tr.row_selected').removeClass('row_selected');
                    $(this).addClass('row_selected');
                }
                // load preview of clicked layout
                designer.Load(event.target.nodeName == "TR" ? event.target.id : event.target.parentNode.id);
            });
        });
    </script>
</head>

<body>

<div style="float: right">
    <div id="themeSwitcher"></div>
</div>

<div id="header">
    <h1>Anno Designer - Web Edition</h1>
    <button id="login">Login</button>
    <button id="register">Register</button>
    <button id="toggleEditor">Toggle editor</button>
</div>

<div id="preview" class="preview">
    <h2>Layout preview</h2>
    <div id="editor"></div>
</div>

<div id="list">
    <h2>Layout list</h2>
    <table id="listTable">
        <thead>
        <tr>
            <th>Name</th>
            <th>Author</th>
            <th>Width</th>
            <th>Height</th>
            <th>Created</th>
            <th>Edited</th>
        </tr>
        </thead>
    </table>
</div>


</body>

</html>