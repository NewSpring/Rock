$(document).ready(function(){
  var wrapperOffset = $('#navigation').height() + $('#navigation-secondary').height();
  $('#group-finder-wrapper').css('top', wrapperOffset + 'px');
  $('#map-canvas').css('top', wrapperOffset + 'px');

  var defaultLatLng = new google.maps.LatLng(34.0374891,-81.0076046); // Default
  
  drawMap(defaultLatLng); // No geolocation support, show default map

  function drawMap(latlng) {
      var myOptions = {
          zoom: 8,
          scrollwheel: false,
          center: latlng,
          styles: [
    {
        "featureType": "landscape.man_made",
        "elementType": "all",
        "stylers": [
            {
                "color": "#faf5ed"
            },
            {
                "lightness": "0"
            },
            {
                "gamma": "1"
            }
        ]
    },
    {
        "featureType": "poi.park",
        "elementType": "geometry.fill",
        "stylers": [
            {
                "color": "#bae5a6"
            }
        ]
    },
    {
        "featureType": "road",
        "elementType": "all",
        "stylers": [
            {
                "weight": "1.00"
            },
            {
                "gamma": "1.8"
            },
            {
                "saturation": "0"
            }
        ]
    },
    {
        "featureType": "road",
        "elementType": "geometry.fill",
        "stylers": [
            {
                "hue": "#ffb200"
            }
        ]
    },
    {
        "featureType": "road.arterial",
        "elementType": "geometry.fill",
        "stylers": [
            {
                "lightness": "0"
            },
            {
                "gamma": "1"
            }
        ]
    },
    {
        "featureType": "transit.station.airport",
        "elementType": "all",
        "stylers": [
            {
                "hue": "#b000ff"
            },
            {
                "saturation": "23"
            },
            {
                "lightness": "-4"
            },
            {
                "gamma": "0.80"
            }
        ]
    },
    {
        "featureType": "water",
        "elementType": "all",
        "stylers": [
            {
                "color": "#a0daf2"
            }
        ]
    }
],
          mapTypeId: google.maps.MapTypeId.ROADMAP
      };
      map = new google.maps.Map(document.getElementById("map-canvas"), myOptions);
      // place the infowindow.  Invisible, without content.
      var infowindow = new google.maps.InfoWindow({
          content: '',
      });

      bounds = new google.maps.LatLngBounds();
  }

  $('#dynamic-content').on('click','a',function(e){
      e.preventDefault();
      var targetUrl = $(this).attr('href'),
          targetTitle = $(this).attr('title');

      window.history.pushState({url: "" + targetUrl + ""}, targetTitle, targetUrl);
      findGroups();
      updateFilters();
  });

  findGroups();
  updateFilters();

});

function updateFilters() {
  var currentUrl = window.location.href;
  var uri = URI(window.location.href);
  var query = uri.search(true);

  $('.js-filters .js-filter').each(function(i, obj) {
    filter = $(obj);
    if (URI(window.location.href).hasQuery(filter.data("key"), filter.data("value"), true)) {
      link = URI(window.location.href).removeSearch(filter.data("key"), filter.data("value"));
      filter.addClass("active").attr('href',link);
    } else {
      link = URI(window.location.href).addSearch(filter.data("key"), filter.data("value"));
      filter.removeClass("active").attr('href',link);
    }
  });
}

function findGroups() {
  var currentUrl = window.location.href;
  var uri = URI(window.location.href);
  var query = uri.query();
  console.log(query);
  $.ajax({
      url: '/api/GroupFinder?' + query,
      dataType: 'json',
      success: function(response) {
        $('#groups').html(" ");
        
        console.log(response);

        if( ! $.isEmptyObject(response) ){

            var markers = response.map(function(item, i) {
                if (item.GroupLocation !== null) {
                    var marker = new google.maps.Marker({
                        position: new google.maps.LatLng(item.GroupLocation.Latitude, item.GroupLocation.Longitude),
                        icon: 'https://s3.amazonaws.com/ns.assets/newspring/mapmarker.png'
                    });

                    google.maps.event.addListener(marker, 'click', function() {
                        window.location.href = "{{groupViewUrl}}" + item.Id;
                    });

                    bounds.extend(marker.position);

                    return marker;
                }
            });

            console.log(markers);

            // Add a marker clusterer to manage the markers.
            var markerCluster = new MarkerClusterer(map, markers,
                {imagePath: 'https://developers.google.com/maps/documentation/javascript/examples/markerclusterer/m'});
            }

            $.each(response, function(i, item) {

                var groupCard = `
                {[ card title:'${ item.Id }' titlesize:'h3' ]}
                `

                $('#groups').append(groupCard);

               

            });
            $('#groups').prepend('<p><span class="label label-default sans-serif letter-spacing-condensed circular">' + response.length + ' groups found</span></p>');            

            map.setOptions({ maxZoom: 15 });
            map.fitBounds(bounds);
            map.setOptions({ maxZoom: null });
        } else {
            $('#groups').html('No Results');
        }
      }
  });
}