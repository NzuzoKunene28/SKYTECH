/* =============================================================
  My Deliveries — MyDeliveriesJS.js
  Place this file in: MyScripts/MyDeliveriesJS.js
  ============================================================= */

function switchTab(tab) {
    /* Update tab buttons */
    document.getElementById("tab-active").classList.toggle("active", tab === "active");
    document.getElementById("tab-completed").classList.toggle("active", tab === "completed");

    /* Show / hide panels */
    document.getElementById("panel-active").classList.toggle("hidden", tab !== "active");
    document.getElementById("panel-completed").classList.toggle("hidden", tab !== "completed");
}