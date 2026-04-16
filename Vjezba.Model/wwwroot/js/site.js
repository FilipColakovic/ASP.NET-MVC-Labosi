// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
(function () {
	var sidebar = document.querySelector("[data-analog-sidebar]");
	if (!sidebar) {
		return;
	}

	var toggle = sidebar.querySelector("[data-analog-sidebar-toggle]");
	var icon = sidebar.querySelector("[data-analog-sidebar-toggle-icon]");
	if (!toggle || !icon) {
		return;
	}

	var storageKey = "analogSidebarCollapsed";

	function applyState(collapsed) {
		sidebar.classList.toggle("is-collapsed", collapsed);
		icon.textContent = collapsed ? "chevron_right" : "chevron_left";
		toggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
		toggle.setAttribute("title", collapsed ? "Expand sidebar" : "Collapse sidebar");
		toggle.setAttribute("aria-label", collapsed ? "Expand sidebar" : "Collapse sidebar");
	}

	var isCollapsed = window.localStorage.getItem(storageKey) === "1";
	applyState(isCollapsed);

	toggle.addEventListener("click", function () {
		isCollapsed = !isCollapsed;
		applyState(isCollapsed);
		window.localStorage.setItem(storageKey, isCollapsed ? "1" : "0");
	});
})();
