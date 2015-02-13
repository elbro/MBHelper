// Filter options need to be saved to the DOM
var minRating;
var maxRating;
var minOdds;
var maxOdds;
var minAvail;
var comm;
var fromDate = new Date();
var toDate = new Date().setDate(fromDate.getDate() + 15);

$(function () {

    initialise();
    
    var today = new Date();
    function init() {
        var filter = $('#filterContent').html();

        setTimeout(function () {            
                               
            $('#dateSlider').dateRangeSlider({
                bounds: {
                    min: today,
                    max: new Date().setDate(today.getDate() + 30)
                },
                defaultValues: {
                    min: fromDate,
                    max: toDate
                },
                range: {
                    min: { days: 1 }
                },
                formatter:function(val){
                    var days = val.getDate(),
                      month = val.getMonth() + 1,
                      year = val.getFullYear();
                    return days + "/" + month + "/" + year;
                }   
            });

            if (minRating != null) {
                $('.popover #txtMinRating').val(minRating + " %");
                $('.popover #txtMaxRating').val(maxRating + " %");

                $('.popover #txtMinOdds').val(minOdds);
                $('.popover #txtMaxOdds').val(maxOdds);
                $('.popover #minLiquidity').val("£" + minAvail);
                $('.popover #betfairComm').val(comm + " %"  );                     
            }

            initFilter();
        }, 1);
        
        return filter + '<div id="dateSlider" class="dateRange"></div>';
    };

   
    $('#btnFilter').popover({
        placement: "bottom",
        title: "Choose filter options and hit refresh",
        html: true,
        content: init
    });

    $("#btnRefresh").click(function () {
        $(this).tooltip('hide');

        if ($('#btnFilter').hasClass("active")) {
            $('#btnFilter').click();
        }

        $('#btnRefresh').button('loading');
        $('#oddsHolder').hide();
        $('#spinner').show();

        loadOdds();
    });

    $('#btnFilter').click(function () {
        if ($(this).hasClass("active")) {
            minRating = $('.popover #txtMinRating').val().replace("%", "").trim();
            maxRating = $('.popover #txtMaxRating').val().replace("%", "").trim();
            minOdds = $('.popover #txtMinOdds').val().trim();
            maxOdds = $('.popover #txtMaxOdds').val().trim();
            minAvail = $('.popover #minLiquidity').val().replace("£", "").trim();
            comm = $('.popover #betfairComm').val().replace("%", "").trim();

            var dateVals = $("#dateSlider").dateRangeSlider("values");

            fromDate = new Date(dateVals.min.getFullYear(), dateVals.min.getMonth(), dateVals.min.getDate());
            toDate = new Date(dateVals.max.getFullYear(), dateVals.max.getMonth(), dateVals.max.getDate());                       
        }
    });

    $('#btnRefresh').tooltip({
        placement: "right",
        title: "Refreshes the displayed odds using the chosen filter options"
    });  

    $('#bookies').multiselect({
        noneSelectedText: "Select Bookmakers",
        header: false,
        minWidth: 150,
        height: "auto",
        selectedText: "Select Bookmakers"
    });
    
    $('#bookies').multiselect("checkAll");
    
    $('#oddsHolder').click(function () {
        if ($('#btnFilter').hasClass("active")) {
            $('#btnFilter').click();
        }
    });        

});

function initFilter() {
    $('.filter input[type=text]').numeric();

    // .toFixed(2);
    $('.percent').blur(function () {
        var val = this.value;
        if (val.indexOf("%") < 0) {
            this.value += " %";
        }
    });

    $('#minLiquidity').blur(function () {
        var val = this.value;
        if (val.indexOf("£") < 0) {
        this.value = "£" + val;
    }
    });

    $('.filter input[type=text]').click(function () {
        this.select();
    });   
}

function initialise() {
    $('#oddsData').dataTable({
    "bStateSave": true,
    "bLengthChange": true,
    "iDisplayLength": 25,
    "aaSorting": [[5, "desc"]],
    "fnDrawCallback": function (oSettings) {
        highlightRating();
    },
    "aoColumnDefs": [
          { 'bSortable': false, 'aTargets': [1] }
    ]    
    });
    
    $('#oddsData th, td, img').tooltip({
        container: 'body'
    });
}

function highlightRating() {
    $('#oddsData tr td:nth-child(6)').each(function () {
        var val = $(this).html();

        if (val > 100)
        $(this).html('<span class="label label-success">' + val + '</span>');
    });
}

function loadOdds() {   
    // Get Filter parameters
    var bookies = $("#bookies").multiselect("getChecked").map(function () { return this.value; }).get();    

    var min = dateFormat(fromDate, "yyyy/mm/dd HH:MM:ss");
    var max = dateFormat(toDate, "yyyy/mm/dd HH:MM:ss");

    jQuery.ajaxSettings.traditional = true;

    $('#results').load('Home',
        {
            bookieIds: bookies,
            minArb: minRating,
            maxArb: maxRating,
            minOdds: minOdds,
            maxOdds: maxOdds,
            minLiquidity: minAvail,
            fromDate: min,
            toDate: max,
            commRate: comm
        },
        function (response, status, xhr) {

            switch (xhr.status) {
                case 200:
                    $('#spinner').hide();
                    initialise();
                    $('#oddsHolder').show();
                    $('#btnRefresh').button('reset');
                    break;
                default:
                    //$('#results').html('<p>' + xhr.status + ': ' + xhr.statusText + '. Please contact the club and let them know.</p>');
                    $('#results').html(xhr.responseText);
                    break;
            }
        });
}
