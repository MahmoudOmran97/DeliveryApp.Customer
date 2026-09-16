namespace DeliveryApp.Customer.Services;

/// <summary>
/// HTML bridge used by MAUI WebViews to render OpenFreeMap through MapLibre GL JS.
/// </summary>
public static class OpenFreeMapHtml
{
    public static string Create(string initialMapScript = "")
    {
        // This is intentionally a non-interpolated raw string. JavaScript uses many
        // braces, and keeping it non-interpolated avoids C# treating them as fields.
        const string html = """
<!doctype html>
<html lang="ar">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no" />
  <link href="https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.css" rel="stylesheet" />
  <style>
    html, body, #map { width:100%; height:100%; margin:0; padding:0; overflow:hidden; }
    .maplibregl-ctrl-attrib { font-size:10px; }
  </style>
</head>
<body>
  <div id="map"></div>
  <script src="https://unpkg.com/maplibre-gl@5/dist/maplibre-gl.js"></script>
  <script>
 maplibregl.setRTLTextPlugin(
  'https://unpkg.com/@mapbox/mapbox-gl-rtl-text@0.2.3/mapbox-gl-rtl-text.js',
  true // lazy: يحمّل الـ plugin بس أول ما يلاقي نص RTL في الخريطة
);
    const map = new maplibregl.Map({
      container: 'map',
      style: 'https://tiles.openfreemap.org/styles/liberty',
      center: [31.2357, 30.0444],
      zoom: 13,
      attributionControl: true
    });

    map.addControl(new maplibregl.NavigationControl({ showCompass: false }), 'top-right');
    let markers = {};
    let routes = {};

    function notify(url) { window.location.href = url; }
    function esc(value) { return encodeURIComponent(String(value)); }

    function setMarker(id, lng, lat, color, symbol) {
      if (markers[id]) markers[id].remove();
      const element = document.createElement('div');
      element.style.width = '36px';
      element.style.height = '36px';
      element.style.borderRadius = '50%';
      element.style.background = color;
      element.style.border = '3px solid white';
      element.style.boxShadow = '0 2px 7px #0008';
      element.style.display = 'flex';
      element.style.alignItems = 'center';
      element.style.justifyContent = 'center';
      element.style.fontSize = '19px';
      element.textContent = symbol || '';
      markers[id] = new maplibregl.Marker({ element: element, anchor: 'center' })
        .setLngLat([lng, lat]).addTo(map);
    }

    function removeMarker(id) {
      if (markers[id]) { markers[id].remove(); delete markers[id]; }
    }

    function setRoute(id, coordinates, color, width) {
      const sourceId = 'source-' + id;
      const layerId = 'route-' + id;
      const data = { type:'Feature', geometry:{ type:'LineString', coordinates:coordinates } };
      if (map.getSource(sourceId)) map.getSource(sourceId).setData(data);
      else map.addSource(sourceId, { type:'geojson', data:data });
      if (!map.getLayer(layerId)) map.addLayer({
        id: layerId, type:'line', source:sourceId,
        layout: { 'line-cap':'round', 'line-join':'round' },
        paint: { 'line-color':color, 'line-width':width, 'line-opacity':0.9 }
      });
      routes[id] = true;
    }

    function fitToPoints(points) {
      if (!points || points.length === 0) return;
      const bounds = new maplibregl.LngLatBounds(points[0], points[0]);
      points.forEach(p => bounds.extend(p));
      map.fitBounds(bounds, { padding: 80, maxZoom: 16, duration: 500 });
    }

    function centerOn(lng, lat, zoom) {
      map.easeTo({ center:[lng,lat], zoom:zoom, duration:400 });
    }

    map.on('load', () => {
      __INITIAL_MAP_SCRIPT__
      notify('app://map-ready');
    });

    map.on('click', (event) => {
      notify('app://map-click?lat=' + esc(event.lngLat.lat) + '&lng=' + esc(event.lngLat.lng));
    });
  </script>
</body>
</html>
""";

        return html.Replace("__INITIAL_MAP_SCRIPT__", initialMapScript,
            StringComparison.Ordinal);
    }
}

public static class WebViewMapQuery
{
    public static bool TryGetCoordinates(string url, out double lat, out double lng)
    {
        lat = lng = 0;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries);
        string? latText = null;
        string? lngText = null;

        foreach (var item in query)
        {
            var pair = item.Split('=', 2);
            if (pair.Length != 2) continue;

            var key = Uri.UnescapeDataString(pair[0]);
            var value = Uri.UnescapeDataString(pair[1]);
            if (key == "lat") latText = value;
            if (key == "lng") lngText = value;
        }

        return double.TryParse(latText, System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out lat)
            && double.TryParse(lngText, System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out lng);
    }
}

public static class OpenFreeMapAttribution
{
    public const string Text = "OpenFreeMap © OpenMapTiles · Data from OpenStreetMap";
}
