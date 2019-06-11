$(document).ready(function(){
  var wrapperOffset = $('#navigation').height() + $('#navigation-secondary').height();
  console.log(wrapperOffset);
  $('#group-finder-wrapper').css('top', wrapperOffset + 'px');

  var defaultLatLng = new google.maps.LatLng(34.0374891,-81.0076046); // Default
  
  drawMap(defaultLatLng); // No geolocation support, show default map

  function drawMap(latlng) {
      var myOptions = {
          zoom: 8,
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
  
  $.ajax({
      url: '/api/GroupFinder?' + query,
      dataType: 'json',
      success: function(response) {
        $('#groups').html(" ");

        if( ! $.isEmptyObject(response) ){
            $.each(response, function(i, item) {
                console.log(item.Id)

                var groupCard = `
                {[ card title:'${ item.Id }' titlesize:'h3' ]}
                `

                console.log(groupCard);

                $('#groups').append(groupCard);

                if (item.GroupLocation !== null) {
                var marker = new google.maps.Marker({
                    position: new google.maps.LatLng(item.GroupLocation.Latitude, item.GroupLocation.Longitude),
                    map: map,
                });
                google.maps.event.addListener(marker, 'click', function() {
                    window.location.href = "{{groupViewUrl}}" + item.Id;
                });
                //extend the bounds to include each marker's position
                bounds.extend(marker.position);
                }

            });

            map.setOptions({ maxZoom: 15 });
            map.fitBounds(bounds);
            map.setOptions({ maxZoom: null });
        } else {
            $('#groups').html('No Results');
        }
        
        // if( ! $.isEmptyObject(response) ){
        //   $.each(response, function(i, item) {
        //       var $tags = "";
        //       $.each(item.Tags, function(t, tag) {
        //         var active = "";
        //         var link = "";
        //         if (URI(window.location.href).hasQuery("Tags", $.trim(tag), true)) {
        //           active = "active";
        //           link = URI(window.location.href).removeSearch("Tags", $.trim(tag));
        //         } else {
        //           link = URI(window.location.href).addSearch("Tags", $.trim(tag));
        //         }
        //         $tags += '<a href="' + link + '" class="stronger text-decoration-none letter-spacing-condensed no-breaks ' + active + '"><i class="fas fa-sm fa-tag"></i> ' + tag + '</a>';
        //       });
        //       if (item.Topic !== null) {
        //         var active = "";
        //         var link = "";
        //         if (URI(window.location.href).hasQuery("topic", $.trim(item.Topic), true)) {
        //           active = "active";
        //           link = URI(window.location.href).removeSearch("topic", $.trim(item.Topic));
        //         } else {
        //           link = URI(window.location.href).addSearch("topic", $.trim(item.Topic));
        //         }
        //         $tags += '<a href="' + link + '" class="stronger text-decoration-none letter-spacing-condensed no-breaks ' + active + '"><i class="fas fa-sm fa-tag"></i> ' + item.Topic + '</a>';
        //       }
        //       if (item.KidFriendly == true) {
        //         var active = "";
        //         var link = "";
        //         if (URI(window.location.href).hasQuery("kidFriendly", "true", true)) {
        //           active = "active";
        //           link = URI(window.location.href).removeSearch("kidFriendly", "true");
        //         } else {
        //           link = URI(window.location.href).addSearch("kidFriendly", "true");
        //         }
        //         $tags += '<a href="' + link + '" class="stronger text-decoration-none letter-spacing-condensed no-breaks ' + active + '"><i class="fas fa-sm fa-tag"></i> Kid Friendly</a>';
        //       }
        //       if (item.Campus !== null) {
        //         var active = "";
        //         var link = "";
        //         if (URI(window.location.href).hasQuery("campuses", $.trim(item.CampusId), true)) {
        //           active = "active";
        //           link = URI(window.location.href).removeSearch("campuses", $.trim(item.CampusId));
        //         } else {
        //           link = URI(window.location.href).addSearch("campuses", $.trim(item.CampusId));
        //         }
        //         $tags += '<a href="' + link + '" class="stronger text-decoration-none letter-spacing-condensed no-breaks ' + active + '"><i class="fas fa-sm fa-tag"></i> ' + item.Campus + '</a>';
        //       }
        //       var photo = item.Photo.replace("<img src='","").replace("' class='img-responsive' />","");
        //       var $tr = $('<div class="panel panel-default">').attr("data-group-id", item.Id).append(
        //         $('<a class="panel-image">').attr("href", "{{groupViewUrl}}"+item.Id).append(
        //             $('<div class="position-relative ratio-landscape background-cover background-center">').css('background-image', 'url(' + photo + ')')
        //         )
        //       ).append(
        //         $('<div class="panel-body">').append(
        //           '<h2 class="h4 push-half-bottom"><a href=""{{groupViewUrl}}"+item.Id">' + item.Name + '</a></h2>',
        //           '<div class="schedule">' + (item.Schedule || "") + '</div>',
        //           '<div class="distance">' + (item.Distance || "") + '</div>',
        //           '<p class="description">' + (item.Description || "") + '</p>',
        //           '<p class="tag-list sans-serif push-half-bottom"><small>' + $tags + '</small></p>'
        //         )
        //       ).appendTo('#groups');
              
        //       if (item.GroupLocation !== null) {
        //         var marker = new google.maps.Marker({
        //             position: new google.maps.LatLng(item.GroupLocation.Latitude, item.GroupLocation.Longitude),
        //             map: map,
        //         });
        //         google.maps.event.addListener(marker, 'click', function() {
        //             window.location.href = "{{groupViewUrl}}" + item.Id;
        //         });
        //         //extend the bounds to include each marker's position
        //         bounds.extend(marker.position);
        //       }

        //   });

        //   map.setOptions({ maxZoom: 15 });
        //   map.fitBounds(bounds);
        //   map.setOptions({ maxZoom: null });
        // } else {
        //   $('#groups').html('No Results');
        // }
      }
  });
}