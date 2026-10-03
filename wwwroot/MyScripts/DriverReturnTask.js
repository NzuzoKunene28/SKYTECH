/* =============================================================
   Driver Return Task View — DriverReturnTaskJS.js
   ============================================================= */

(function () {

    /* ── Read hidden fields injected by Razor ───────────────── */
    var orderItemId = document.getElementById("js-orderitem-id").value;
    var taskType = document.getElementById("js-task-type").value; // "Collection" or "Re-delivery"
    var deliveryAddress = document.getElementById("js-address").value;
    var token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    /* ── Map init ───────────────────────────────────────────── */
    var map = L.map("map").setView([-26.195246, 28.034088], 12); // Default Joburg
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: "&copy; OpenStreetMap contributors"
    }).addTo(map);

    var routeLayer = null;
    var startMarker = null;
    var endMarker = null;
    var liveMarker = null;
    var watchId = null;

    /* ── Geocode an address ─────────────────────────────────── */
    function geocode(address) {
        var url = "https://nominatim.openstreetmap.org/search?format=json&countrycodes=za&q="
            + encodeURIComponent(address);
        return fetch(url)
            .then(function (res) { return res.json(); })
            .then(function (arr) {
                if (!arr || arr.length === 0) return null;
                return {
                    lat: parseFloat(arr[0].lat),
                    lon: parseFloat(arr[0].lon),
                    display_name: arr[0].display_name
                };
            });
    }

    /* ── Get driving route from OSRM ───────────────────────── */
    function getRoute(start, end) {
        var url = "https://router.project-osrm.org/route/v1/driving/"
            + start.lon + "," + start.lat + ";"
            + end.lon + "," + end.lat
            + "?overview=full&geometries=geojson";
        return fetch(url)
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data || data.code !== "Ok" || !data.routes || !data.routes.length)
                    throw new Error("No route found");
                return {
                    route: data.routes[0].geometry,
                    distance: data.routes[0].distance,
                    duration: data.routes[0].duration
                };
            });
    }

    /* ── Draw route on map ──────────────────────────────────── */
    function drawRoute(startCoord, endCoord, endDisplayName, routeColor) {
        return getRoute(startCoord, endCoord)
            .then(function (summary) {
                if (routeLayer) { map.removeLayer(routeLayer); routeLayer = null; }
                if (startMarker) { map.removeLayer(startMarker); startMarker = null; }
                if (endMarker) { map.removeLayer(endMarker); endMarker = null; }

                routeLayer = L.geoJSON(summary.route, {
                    style: { color: routeColor || "#1a73e8", weight: 5 }
                }).addTo(map);

                startMarker = L.marker([startCoord.lat, startCoord.lon])
                    .bindPopup("<b>📍 Your Location</b>").addTo(map).openPopup();

                endMarker = L.marker([endCoord.lat, endCoord.lon])
                    .bindPopup("<b>🏠 Customer</b><br/>" + (endDisplayName || ""))
                    .addTo(map);

                var coords = summary.route.coordinates.map(function (c) { return [c[1], c[0]]; });
                if (coords.length) map.fitBounds(coords, { padding: [40, 40] });

                // Show route info in a small banner (optional, you can add a div for this)
                var routeInfo = document.getElementById("routeInfoText");
                if (routeInfo) {
                    routeInfo.textContent = "Distance: " + (summary.distance / 1000).toFixed(2)
                        + " km  ·  Est. " + Math.ceil(summary.duration / 60) + " min";
                }
            })
            .catch(function (err) {
                alert("Could not load route: " + err.message);
            });
    }

    /* ── Start live GPS tracking (optional) ─────────────────── */
    function startLiveTracking() {
        if (watchId || !navigator.geolocation) return;
        watchId = navigator.geolocation.watchPosition(
            function (pos) {
                var lat = pos.coords.latitude, lon = pos.coords.longitude;
                if (!liveMarker) {
                    liveMarker = L.marker([lat, lon], {
                        icon: L.icon({
                            iconUrl: "https://maps.google.com/mapfiles/ms/icons/blue-dot.png",
                            iconSize: [32, 32]
                        })
                    }).addTo(map).bindPopup("You (Driver)");
                } else {
                    liveMarker.setLatLng([lat, lon]);
                }
            },
            function (err) { console.warn("GPS error:", err.message); },
            { enableHighAccuracy: true, maximumAge: 2000, timeout: 10000 }
        );
    }

    /* ── Load Route button handler ──────────────────────────── */
    window.loadMapRoute = function () {
        if (!deliveryAddress) {
            alert("No delivery address available.");
            return;
        }

        if (!navigator.geolocation) {
            alert("Geolocation is not supported by your browser.");
            return;
        }

        navigator.geolocation.getCurrentPosition(
            function (pos) {
                var driverCoord = { lat: pos.coords.latitude, lon: pos.coords.longitude };
                geocode(deliveryAddress).then(function (dest) {
                    if (!dest) {
                        alert("Could not resolve customer address.");
                        return;
                    }
                    // Use different colors for collection vs re-delivery
                    var color = (taskType === "Collection") ? "#d97706" : "#0891b2";
                    drawRoute(driverCoord, dest, dest.display_name, color);
                    startLiveTracking();
                });
            },
            function () {
                alert("Unable to get your location. Please allow location access.");
            }
        );
    };

    /* ── POST helper for task completion ────────────────────── */
    function postAction(url, bodyParams) {
        return fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: bodyParams + "&__RequestVerificationToken=" + encodeURIComponent(token)
        }).then(function (res) { return res.json(); });
    }

    /* ── Mark as Collected ──────────────────────────────────── */
    window.markCollected = function () {
        if (!confirm("Confirm that you have collected this item from the customer?")) return;
        var btn = document.getElementById("collectedBtn");
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';

        postAction("/DeliveryDriver/MarkCollected", "orderItemId=" + encodeURIComponent(orderItemId))
            .then(function (data) {
                if (data.success) {
                    document.getElementById("completionBanner").style.display = "flex";
                    if (watchId) { navigator.geolocation.clearWatch(watchId); watchId = null; }
                    setTimeout(function () { window.location.href = "/DeliveryDriver/MyTasks"; }, 2000);
                } else {
                    alert(data.reason || "Failed to mark collected.");
                    btn.disabled = false;
                    btn.innerHTML = '<i class="fas fa-box-open"></i> Mark as Collected';
                }
            });
    };

    /* ── Mark as Re-delivered ───────────────────────────────── */
    window.markReDelivered = function () {
        if (!confirm("Confirm that you have delivered the replacement item to the customer?")) return;
        var btn = document.getElementById("deliveredBtn");
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';

        // Optional: prompt for pin
        // var pin = prompt("Enter delivery pin (if required):");
        // var body = "orderItemId=" + encodeURIComponent(orderItemId) + "&pin=" + encodeURIComponent(pin || "");

        postAction("/DeliveryDriver/MarkReDelivered", "orderItemId=" + encodeURIComponent(orderItemId))
            .then(function (data) {
                if (data.success) {
                    document.getElementById("completionBanner").style.display = "flex";
                    if (watchId) { navigator.geolocation.clearWatch(watchId); watchId = null; }
                    setTimeout(function () { window.location.href = "/DeliveryDriver/MyTasks"; }, 2000);
                } else {
                    alert(data.reason || "Failed to complete re-delivery.");
                    btn.disabled = false;
                    btn.innerHTML = '<i class="fas fa-circle-check"></i> Mark as Delivered';
                }
            });
    };

    /* ── Boot: show customer location marker on page load ───── */
    document.addEventListener("DOMContentLoaded", function () {
        if (deliveryAddress) {
            geocode(deliveryAddress).then(function (dest) {
                if (dest) {
                    if (endMarker) map.removeLayer(endMarker);
                    endMarker = L.marker([dest.lat, dest.lon])
                        .addTo(map)
                        .bindPopup("<b>Customer Address</b><br/>" + dest.display_name)
                        .openPopup();
                    map.setView([dest.lat, dest.lon], 14);
                }
            });
        }

        // Optional: Add a route info bar element if missing
        if (!document.getElementById("routeInfoText")) {
            var bar = document.createElement("div");
            bar.id = "route-info";
            bar.className = "route-info-bar";
            bar.style = "display:none; margin:0.5rem 0; padding:0.5rem 1rem; background:#f0f4ff; border-radius:8px;";
            bar.innerHTML = '<i class="fas fa-route"></i> <span id="routeInfoText"></span>';
            var mapContainer = document.querySelector(".map-container");
            if (mapContainer) mapContainer.parentNode.insertBefore(bar, mapContainer);
        }
    });

})();