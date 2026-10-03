/* =============================================================
  Order Tracking Page — TrackOrderJS.js
  Place this file in: MyScripts/TrackOrderJS.js
  ============================================================= */

(function () {

    /* ── State ─────────────────────────────────────────────── */
    var currentStep = parseInt(document.getElementById("js-current-step").value, 10);
    var orderId = document.getElementById("js-order-id").value;
    var pollInterval = null;
    var archiveTimer = null;

    /* ── Smooth swap to empty state after archive ───────────── */
    function swapToEmptyState() {
        var body = document.querySelector(".ot-body");
        if (!body) return;

        /* Fade out current content */
        body.style.transition = "opacity 0.5s ease";
        body.style.opacity = "0";

        setTimeout(function () {
            /* Replace entire body content with empty state */
            body.innerHTML = [
                '<div class="ot-card" style="text-align:center; padding:2.5rem 1.5rem; margin-top:1rem;">',
                '  <svg width="48" height="48" viewBox="0 0 24 24" fill="none"',
                '       stroke="currentColor" stroke-width="1.5"',
                '       stroke-linecap="round" stroke-linejoin="round"',
                '       style="margin-bottom:1rem; opacity:.35;">',
                '    <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/>',
                '    <line x1="3" y1="6" x2="21" y2="6"/>',
                '    <path d="M16 10a4 4 0 0 1-8 0"/>',
                '  </svg>',
                '  <p style="margin:0 0 0.4rem; font-size:1rem; font-weight:600; color:#111;">',
                '    No active orders',
                '  </p>',
                '  <p style="margin:0 0 1.6rem; font-size:0.85rem; color:#666; line-height:1.5;">',
                '    Your order has been delivered and moved to your order history.',
                '  </p>',
                '  <a href="/Customer/MyOrders"',
                '     style="display:inline-flex;align-items:center;gap:6px;background:#1D9E75;',
                '            color:#fff;font-family:\'DM Sans\',sans-serif;font-size:13px;',
                '            font-weight:500;padding:11px 24px;border-radius:10px;',
                '            text-decoration:none;">',
                '    View My Orders',
                '  </a>',
                '</div>'
            ].join("\n");

            /* Fade back in */
            body.style.opacity = "1";

            /* Also update the hidden step so JS knows there is nothing active */
            var stepEl = document.getElementById("js-current-step");
            if (stepEl) stepEl.value = "-1";

        }, 500);
    }

    /* ── Archive order on server then swap UI ───────────────── */
    function archiveAndClear() {
        fetch("/Customer/ArchiveOrder?orderId=" + encodeURIComponent(orderId), {
            method: "POST",
            headers: { "Content-Type": "application/json" }
        })
            .then(function (res) {
                if (!res.ok) throw new Error("Archive failed");
                return res.json();
            })
            .then(function (data) {
                if (data && data.success) {
                    swapToEmptyState();
                }
            })
            .catch(function () {
                /* Even if the request fails, still swap the UI */
                swapToEmptyState();
            });
    }

    /* ── Apply a new step to the UI ────────────────────────── */
    function applyStep(step) {
        if (step === currentStep) return;
        currentStep = step;

        for (var i = 0; i < 4; i++) {
            var state = i < step ? "done" : i === step ? "active" : "pending";

            var icon = document.getElementById("icon-" + i);
            if (icon) icon.className = "ot-icon " + state;

            var name = document.getElementById("name-" + i);
            if (name) name.className = "ot-step-name " + state;

            var line = document.getElementById("line-" + i);
            if (line) line.style.height = (i < step ? "100" : "0") + "%";

            var strip = document.getElementById("strip-" + i);
            if (strip) strip.style.width = (i < step ? "100%" : i === step ? "50%" : "0%");

            var badge = document.getElementById("badge-" + i);
            if (badge) {
                badge.style.display = (state === "active" && i < 3) ? "inline-block" : "none";
            }
        }

        /* Delivered — show banner, start 60s archive timer */
        if (step >= 3) {
            var etaEl = document.getElementById("etaValue");
            if (etaEl) {
                etaEl.textContent = "Delivered \u2713";
                etaEl.classList.add("delivered");
            }
            var deliveredBanner = document.getElementById("deliveredBanner");
            if (deliveredBanner) deliveredBanner.classList.add("visible");

            stopPolling();
            startArchiveTimer();
        }

        /* Fade out driver card once past Assigned */
        if (step > 1) {
            var dc = document.getElementById("driverCard");
            if (dc) {
                dc.style.opacity = "0";
                setTimeout(function () {
                    if (dc.parentNode) dc.parentNode.removeChild(dc);
                }, 500);
            }
        }
    }

    /* ── Start the 60s archive timer ───────────────────────── */
    function startArchiveTimer() {
        if (archiveTimer) return; /* prevent double-starting */
        archiveTimer = setTimeout(function () {
            archiveAndClear();
        }, 60000); /* 60 seconds */
    }

    /* ── Poll server every 15 seconds ──────────────────────── */
    function pollStatus() {
        fetch("/Customer/GetOrderStatus?orderId=" + encodeURIComponent(orderId))
            .then(function (res) {
                if (!res.ok) throw new Error("Bad response");
                return res.json();
            })
            .then(function (data) {
                if (data && typeof data.currentStep === "number") {
                    applyStep(data.currentStep);
                }
            })
            .catch(function () { /* silent retry */ });
    }

    function startPolling() {
        if (currentStep >= 3) {
            /* Already delivered when page loaded — start archive timer immediately */
            startArchiveTimer();
        } else {
            pollInterval = setInterval(pollStatus, 15000);
        }
    }

    function stopPolling() {
        if (pollInterval) {
            clearInterval(pollInterval);
            pollInterval = null;
        }
    }

    /* ── Boot ──────────────────────────────────────────────── */
    document.addEventListener("DOMContentLoaded", function () {
        startPolling();
    });

})();