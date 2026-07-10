// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Sidebar collapse/expand behavior with persisted state in localStorage.
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
	var storageEnabled = true;

	// Safely reads a value from localStorage and disables storage usage on failure.
	function readStorageValue(key) {
		if (!storageEnabled) {
			return null;
		}

		try {
			return window.localStorage.getItem(key);
		} catch (error) {
			storageEnabled = false;
			return null;
		}
	}

	// Safely writes a value to localStorage and disables storage usage on failure.
	function writeStorageValue(key, value) {
		if (!storageEnabled) {
			return;
		}

		try {
			window.localStorage.setItem(key, value);
		} catch (error) {
			storageEnabled = false;
		}
	}

	// Applies collapsed/expanded sidebar classes and related accessibility attributes.
	function applyState(collapsed) {
		document.documentElement.classList.toggle("analog-sidebar-collapsed", collapsed);
		sidebar.classList.toggle("is-collapsed", collapsed);
		icon.textContent = collapsed ? "chevron_right" : "chevron_left";
		toggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
		toggle.setAttribute("title", collapsed ? "Expand sidebar" : "Collapse sidebar");
		toggle.setAttribute("aria-label", collapsed ? "Expand sidebar" : "Collapse sidebar");
	}

	var isCollapsed = readStorageValue(storageKey) === "1";
	applyState(isCollapsed);

	toggle.addEventListener("click", function () {
		isCollapsed = !isCollapsed;
		applyState(isCollapsed);
		writeStorageValue(storageKey, isCollapsed ? "1" : "0");
	});
})();

// Mobile/tablet side menu behavior.
(function () {
	var sidebar = document.querySelector("[data-analog-sidebar]");
	var openButton = document.querySelector("[data-analog-sidebar-mobile-toggle]");
	var closeButton = document.querySelector("[data-analog-sidebar-mobile-close]");
	var backdrop = document.querySelector("[data-analog-sidebar-backdrop]");
	if (!sidebar || !openButton) {
		return;
	}

	function setOpen(isOpen) {
		document.documentElement.classList.toggle("analog-sidebar-open", isOpen);
		openButton.setAttribute("aria-expanded", isOpen ? "true" : "false");
		sidebar.setAttribute("aria-hidden", isOpen ? "false" : "true");
	}

	openButton.addEventListener("click", function () {
		setOpen(true);
	});

	if (closeButton) {
		closeButton.addEventListener("click", function () {
			setOpen(false);
		});
	}

	if (backdrop) {
		backdrop.addEventListener("click", function () {
			setOpen(false);
		});
	}

	sidebar.querySelectorAll("a").forEach(function (link) {
		link.addEventListener("click", function () {
			setOpen(false);
		});
	});

	document.addEventListener("keydown", function (event) {
		if (event.key === "Escape") {
			setOpen(false);
		}
	});
})();

// Turns manifest rows into compact accordions on mobile screens.
(function () {
	var table = document.querySelector("#manifest .analog-manifest-table");
	if (!table) {
		return;
	}

	var mobileQuery = window.matchMedia("(max-width: 640px)");

	function collapseRowsWhenDesktop() {
		if (mobileQuery.matches) {
			return;
		}

		table.querySelectorAll("tr.is-expanded").forEach(function (row) {
			row.classList.remove("is-expanded");
			row.setAttribute("aria-expanded", "false");
		});
	}

	table.querySelectorAll("tbody tr[data-row-id]").forEach(function (row) {
		row.setAttribute("tabindex", "0");
		row.setAttribute("aria-expanded", "false");
	});

	table.addEventListener("click", function (event) {
		if (!mobileQuery.matches) {
			return;
		}

		var row = event.target.closest("tr[data-row-id]");
		if (!row || !table.contains(row)) {
			return;
		}

		if (event.target.closest("button, form, input, select, textarea, [data-edit-open], [data-create-open]")) {
			return;
		}

		if (event.target.closest("a")) {
			event.preventDefault();
		}

		var isExpanded = row.classList.toggle("is-expanded");
		row.setAttribute("aria-expanded", isExpanded ? "true" : "false");
	});

	table.addEventListener("keydown", function (event) {
		if (!mobileQuery.matches || (event.key !== "Enter" && event.key !== " ")) {
			return;
		}

		var row = event.target.closest("tr[data-row-id]");
		if (!row || !table.contains(row)) {
			return;
		}

		event.preventDefault();
		var isExpanded = row.classList.toggle("is-expanded");
		row.setAttribute("aria-expanded", isExpanded ? "true" : "false");
	});

	if (typeof mobileQuery.addEventListener === "function") {
		mobileQuery.addEventListener("change", collapseRowsWhenDesktop);
	} else if (typeof mobileQuery.addListener === "function") {
		mobileQuery.addListener(collapseRowsWhenDesktop);
	}
})();

// Replaces select inputs with searchable autocomplete controls backed by server endpoints.
(function () {
	var selects = document.querySelectorAll("select[data-autocomplete-source]");
	if (!selects.length) {
		return;
	}

	selects.forEach(function (select) {
		if (select.getAttribute("data-autocomplete-init") === "1") {
			return;
		}

		var source = select.getAttribute("data-autocomplete-source");
		if (!source) {
			return;
		}

		select.setAttribute("data-autocomplete-init", "1");
		select.style.display = "none";
		select.setAttribute("aria-hidden", "true");

		var wrapper = document.createElement("div");
		wrapper.className = "relative";
		var searchInput = document.createElement("input");
		searchInput.type = "text";
		searchInput.className = select.className;
		searchInput.autocomplete = "off";
		searchInput.placeholder = select.getAttribute("data-autocomplete-placeholder") || "Search...";
		searchInput.setAttribute("aria-label", "Search dropdown options");

		var panel = document.createElement("div");
		panel.className = "absolute z-30 mt-1 w-full max-h-56 overflow-auto rounded-md border border-outline-variant/30 bg-[#f3efe9] shadow-xl hidden";

		select.parentNode.insertBefore(wrapper, select);
		wrapper.appendChild(searchInput);
		wrapper.appendChild(panel);

		// Closes the suggestion panel and clears existing options.
		function closePanel() {
			panel.classList.add("hidden");
			panel.innerHTML = "";
		}

		// Returns the currently selected option text from the hidden native select.
		function getSelectedText() {
			var selectedOption = select.options[select.selectedIndex];
			if (!selectedOption || !selectedOption.value) {
				return "";
			}

			return selectedOption.text;
		}

		// Applies a picked autocomplete item to the underlying select and syncs UI state.
		function setSelection(item) {
			var value = String(item.id);
			var option = select.querySelector('option[value="' + value + '"]');
			if (!option) {
				option = document.createElement("option");
				option.value = value;
				option.text = item.text;
				select.appendChild(option);
			}

			select.value = value;
			searchInput.value = item.text;
			select.dispatchEvent(new Event("change", { bubbles: true }));
			closePanel();
		}

		// Renders autocomplete suggestions inside the floating panel.
		function renderItems(items) {
			panel.innerHTML = "";
			if (!items.length) {
				closePanel();
				return;
			}

			items.forEach(function (item) {
				if (!item || typeof item.id === "undefined" || typeof item.text !== "string") {
					return;
				}

				var optionButton = document.createElement("button");
				optionButton.type = "button";
				optionButton.className = "block w-full px-3 py-2 text-left text-sm text-on-surface bg-[#f3efe9] hover:bg-[#e7e1d9] transition-colors border-b border-outline-variant/20 last:border-b-0";
				optionButton.textContent = item.text;
				optionButton.addEventListener("mousedown", function (event) {
					event.preventDefault();
					setSelection(item);
				});
				panel.appendChild(optionButton);
			});

			if (!panel.childElementCount) {
				closePanel();
				return;
			}

			panel.classList.remove("hidden");
		}

		searchInput.value = getSelectedText();

		select.addEventListener("change", function () {
			var selectedText = getSelectedText();
			if (selectedText !== searchInput.value) {
				searchInput.value = selectedText;
			}
		});
		if (select.form) {
			select.form.addEventListener("reset", function () {
				window.setTimeout(function () {
					searchInput.value = getSelectedText();
					closePanel();
				}, 0);
			});
		}

		var debounceHandle = 0;
		var latestRequestId = 0;

		// Debounced search that fetches suggestions and ignores stale responses.
		function executeSearch() {
			window.clearTimeout(debounceHandle);
			debounceHandle = window.setTimeout(function () {
				var query = searchInput.value || "";
				if (!query.trim()) {
					closePanel();
				}

				// Keep model value valid only when an item was explicitly picked.
				if (query !== getSelectedText()) {
					select.value = "";
				}

				var requestId = ++latestRequestId;
				var endpoint = "/autocomplete/" + encodeURIComponent(source) + "?q=" + encodeURIComponent(query);

				fetch(endpoint, {
					headers: {
						"X-Requested-With": "XMLHttpRequest"
					}
				})
					.then(function (response) {
						if (!response.ok) {
							throw new Error("Autocomplete request failed.");
						}
						return response.json();
					})
					.then(function (items) {
						if (requestId !== latestRequestId) {
							return;
						}

						var safeItems = Array.isArray(items) ? items : [];
						renderItems(safeItems);
					})
					.catch(function () {
						closePanel();
					});
			}, 180);
		}

		searchInput.addEventListener("blur", function () {
			window.setTimeout(function () {
				closePanel();
			}, 120);
		});
		searchInput.addEventListener("input", executeSearch);
		searchInput.addEventListener("focus", executeSearch);
	});
})();

// Enables flatpickr date-time controls for inputs marked with data-datetime-picker.
(function () {
	var dateInputs = document.querySelectorAll('input[data-datetime-picker="true"]');
	if (!dateInputs.length || typeof flatpickr !== "function") {
		return;
	}

	var documentLang = (document.documentElement.lang || "en").toLowerCase();
	var useCroatianLocale = documentLang.startsWith("hr");

	if (useCroatianLocale && window.flatpickr && window.flatpickr.l10ns && window.flatpickr.l10ns.hr) {
		window.flatpickr.localize(window.flatpickr.l10ns.hr);
	}

	dateInputs.forEach(function (input) {
		if (input._flatpickr) {
			return;
		}

		flatpickr(input, {
			enableTime: true,
			allowInput: true,
			time_24hr: useCroatianLocale,
			dateFormat: "Y-m-d\\TH:i",
			altInput: true,
			altFormat: useCroatianLocale ? "d.m.Y H:i" : "m/d/Y h:i K"
		});
	});
})();

// Handles create modal lifecycle, validation, and AJAX form submission.
(function () {
	var modals = document.querySelectorAll("[data-create-modal]");
	if (!modals.length) {
		return;
	}

	// Basic client-side email format validation.
	function isEmail(value) {
		return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
	}

	// Clears field-level and summary-level validation messages for a form.
	function clearErrors(form, summary) {
		var errorSpans = form.querySelectorAll("[data-valmsg-for]");
		errorSpans.forEach(function (span) {
			span.textContent = "";
		});
		if (summary) {
			summary.textContent = "";
		}
	}

	function setFieldError(form, fieldName, message) {
		var span = form.querySelector('[data-valmsg-for="' + fieldName + '"]');
		if (span) {
			span.textContent = message;
		}
	}

	function validateField(form, input) {
		if (!input.name) {
			return true;
		}

		var value = (input.value || "").trim();
		var requiredMessage = input.getAttribute("data-val-required");
		var emailMessage = input.getAttribute("data-val-email");
		var rangeMessage = input.getAttribute("data-val-range");
		var rangeMin = input.getAttribute("data-val-range-min");
		var rangeMax = input.getAttribute("data-val-range-max");
		var isValid = true;

		if (requiredMessage && input.type !== "checkbox" && value.length === 0) {
			setFieldError(form, input.name, requiredMessage);
			return false;
		}

		if (emailMessage && value.length > 0 && !isEmail(value)) {
			setFieldError(form, input.name, emailMessage);
			isValid = false;
		}

		if (rangeMessage) {
			if (value.length === 0) {
				setFieldError(form, input.name, rangeMessage);
				isValid = false;
			} else {
				var numericValue = Number(value);
				var minValue = rangeMin ? Number(rangeMin) : null;
				var maxValue = rangeMax ? Number(rangeMax) : null;
				if (Number.isNaN(numericValue)) {
					setFieldError(form, input.name, rangeMessage);
					isValid = false;
				} else if (minValue !== null && numericValue < minValue) {
					setFieldError(form, input.name, rangeMessage);
					isValid = false;
				} else if (maxValue !== null && numericValue > maxValue) {
					setFieldError(form, input.name, rangeMessage);
					isValid = false;
				}
			}
		}

		return isValid;
	}

	function validateForm(form) {
		var inputs = form.querySelectorAll("input, select, textarea");
		var ok = true;
		inputs.forEach(function (input) {
			var fieldOk = validateField(form, input);
			if (!fieldOk) {
				ok = false;
			}
		});
		return ok;
	}

	// Generates a simple random tracking number for package creation defaults.
	function generateTrackingNumber() {
		var value = Math.floor(Math.random() * 1000000).toString().padStart(6, "0");
		return "TN-" + value;
	}

	// Formats a Date into the server-expected local datetime input format.
	function formatLocalDateTime(value) {
		// Pads numeric date/time parts to two digits.
		function pad(number) {
			return number.toString().padStart(2, "0");
		}

		return value.getFullYear() + "-" +
			pad(value.getMonth() + 1) + "-" +
			pad(value.getDate()) + "T" +
			pad(value.getHours()) + ":" +
			pad(value.getMinutes());
	}

	// Detects unset sentinel values from .NET default DateTime payloads.
	function isEmptyDateValue(value) {
		return !value || value.indexOf("0001-01-01") === 0;
	}

	// Sets input value and syncs flatpickr instance when present.
	function setInputValue(input, value) {
		if (!input) {
			return;
		}

		if (input._flatpickr) {
			input._flatpickr.setDate(value, true, "Y-m-d\\TH:i");
			return;
		}

		input.value = value;
	}

	modals.forEach(function (modal) {
		var form = modal.querySelector("[data-create-form]");
		if (!form) {
			return;
		}

		var modalKey = modal.getAttribute("data-create-modal");
		if (!modalKey) {
			return;
		}

		var summary = modal.querySelector("[data-create-summary]");
		var openButtons = document.querySelectorAll('[data-create-open="' + modalKey + '"]');
		var closeButtons = modal.querySelectorAll("[data-create-close]");

		// Opens create modal and applies per-entity default values.
		function openModal() {
			modal.classList.remove("is-hidden");
			modal.setAttribute("aria-hidden", "false");
			if (modalKey === "package") {
				var trackingInput = form.querySelector("[name='TrackingNumber']");
				if (trackingInput && !trackingInput.value.trim()) {
					trackingInput.value = generateTrackingNumber();
				}
			}
			if (modalKey === "delivery") {
				var departureInput = form.querySelector("[name='DepartureDate']");
				var arrivalInput = form.querySelector("[name='ArrivalDate']");
				var now = new Date();
				var departureBase = new Date(now.getTime() + 2 * 24 * 60 * 60 * 1000);
				if (departureInput && isEmptyDateValue(departureInput.value)) {
					setInputValue(departureInput, formatLocalDateTime(departureBase));
				}
				var departureValue = departureInput && !isEmptyDateValue(departureInput.value)
					? new Date(departureInput.value)
					: departureBase;
				if (arrivalInput && isEmptyDateValue(arrivalInput.value)) {
					var arrivalValue = new Date(departureValue.getTime());
					arrivalValue.setDate(arrivalValue.getDate() + 7);
					setInputValue(arrivalInput, formatLocalDateTime(arrivalValue));
				}
			}
			if (modalKey === "statuslog") {
				var timeInput = form.querySelector("[name='TimeChanged']");
				if (timeInput && isEmptyDateValue(timeInput.value)) {
					setInputValue(timeInput, formatLocalDateTime(new Date()));
				}
			}
			var firstInput = form.querySelector("input, select, textarea");
			if (firstInput) {
				firstInput.focus();
			}
		}

		function closeModal() {
			modal.classList.add("is-hidden");
			modal.setAttribute("aria-hidden", "true");
			clearErrors(form, summary);
			form.reset();
		}

		openButtons.forEach(function (button) {
			button.addEventListener("click", openModal);
		});

		closeButtons.forEach(function (button) {
			button.addEventListener("click", closeModal);
		});

		modal.addEventListener("click", function (event) {
			if (event.target && event.target.hasAttribute("data-create-close")) {
				closeModal();
			}
		});

		form.querySelectorAll("input, select, textarea").forEach(function (input) {
			input.addEventListener("blur", function () {
				setFieldError(form, input.name, "");
				validateField(form, input);
			});
		});

		form.addEventListener("submit", function (event) {
			event.preventDefault();
			clearErrors(form, summary);

			if (!validateForm(form)) {
				return;
			}

			var formData = new FormData(form);
			fetch(form.action, {
				method: "POST",
				body: formData,
				headers: {
					"X-Requested-With": "XMLHttpRequest"
				}
			})
				.then(function (response) {
					return response.json().then(function (payload) {
						return { ok: response.ok, payload: payload };
					});
				})
				.then(function (result) {
					if (!result.ok) {
						if (result.payload && result.payload.errors) {
							Object.keys(result.payload.errors).forEach(function (key) {
								var messages = result.payload.errors[key];
								if (messages && messages.length > 0) {
									setFieldError(form, key, messages[0]);
								}
							});
						} else if (summary) {
							summary.textContent = "Unable to create record right now.";
						}
						return;
					}

				closeModal();
				window.location.reload();
			})
			.catch(function () {
				if (summary) {
					summary.textContent = "Unable to create record right now.";
				}
			});
		});
	});
})();

// Handles edit modal lifecycle, prefill mapping, validation, and AJAX form submission.
(function () {
	var modals = document.querySelectorAll("[data-edit-modal]");
	if (!modals.length) {
		return;
	}

	// Basic client-side email format validation.
	function isEmail(value) {
		return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
	}

	// Converts input field names to data-edit-* attribute keys used by edit buttons.
	function toEditDataKey(fieldName) {
		return "data-edit-" + fieldName
			.replace(/\[(\d+)\]/g, "-$1")
			.replace(/\./g, "-")
			.replace(/([a-z0-9])([A-Z])/g, "$1-$2")
			.toLowerCase();
	}

	// Clears field-level and summary-level validation messages for a form.
	function clearErrors(form, summary) {
		var errorSpans = form.querySelectorAll("[data-valmsg-for]");
		errorSpans.forEach(function (span) {
			span.textContent = "";
		});
		if (summary) {
			summary.textContent = "";
		}
	}

	// Sets an error message for a specific form field if its validation span exists.
	function setFieldError(form, fieldName, message) {
		var span = form.querySelector('[data-valmsg-for="' + fieldName + '"]');
		if (span) {
			span.textContent = message;
		}
	}

	// Validates one input using MVC unobtrusive metadata attributes.
	function validateField(form, input) {
		if (!input.name) {
			return true;
		}

		var isCheckbox = input.type === "checkbox";
		var value = isCheckbox ? (input.checked ? "true" : "") : (input.value || "").trim();
		var requiredMessage = input.getAttribute("data-val-required");
		var emailMessage = input.getAttribute("data-val-email");
		var rangeMessage = input.getAttribute("data-val-range");
		var rangeMin = input.getAttribute("data-val-range-min");
		var rangeMax = input.getAttribute("data-val-range-max");
		var isValid = true;

		if (requiredMessage && !isCheckbox && value.length === 0) {
			setFieldError(form, input.name, requiredMessage);
			return false;
		}

		if (emailMessage && value.length > 0 && !isEmail(value)) {
			setFieldError(form, input.name, emailMessage);
			isValid = false;
		}

		if (rangeMessage) {
			if (value.length === 0) {
				setFieldError(form, input.name, rangeMessage);
				isValid = false;
			} else {
				var numericValue = Number(value);
				var minValue = rangeMin ? Number(rangeMin) : null;
				var maxValue = rangeMax ? Number(rangeMax) : null;
				if (Number.isNaN(numericValue)) {
					setFieldError(form, input.name, rangeMessage);
					isValid = false;
				} else if (minValue !== null && numericValue < minValue) {
					setFieldError(form, input.name, rangeMessage);
					isValid = false;
				} else if (maxValue !== null && numericValue > maxValue) {
					setFieldError(form, input.name, rangeMessage);
					isValid = false;
				}
			}
		}

		return isValid;
	}

	// Runs client-side validation for all fields in the form.
	function validateForm(form) {
		var inputs = form.querySelectorAll("input, select, textarea");
		var ok = true;
		inputs.forEach(function (input) {
			var fieldOk = validateField(form, input);
			if (!fieldOk) {
				ok = false;
			}
		});
		return ok;
	}

	// Prefills edit form fields using data-edit-* attributes from the clicked action button.
	function applyButtonDataToForm(form, button) {
		var fields = form.querySelectorAll("input[name], select[name], textarea[name]");
		fields.forEach(function (field) {
			if (field.type === "hidden" && field.name !== "Id") {
				return;
			}

			var dataKey = toEditDataKey(field.name);
			if (!button.hasAttribute(dataKey)) {
				return;
			}

			var value = button.getAttribute(dataKey) || "";
			if (field.type === "checkbox") {
				field.checked = value.toLowerCase() === "true" || value === "1";
				field.dispatchEvent(new Event("change", { bubbles: true }));
				return;
			}

			if (field.getAttribute("data-datetime-picker") === "true" && field._flatpickr) {
				field._flatpickr.setDate(value, true, "Y-m-d\\TH:i");
				field.dispatchEvent(new Event("change", { bubbles: true }));
				return;
			}

			field.value = value;
			field.dispatchEvent(new Event("change", { bubbles: true }));
		});
	}

	modals.forEach(function (modal) {
		var form = modal.querySelector("[data-edit-form]");
		if (!form) {
			return;
		}

		var modalKey = modal.getAttribute("data-edit-modal");
		if (!modalKey) {
			return;
		}

		var summary = modal.querySelector("[data-edit-summary]");
		var openButtons = document.querySelectorAll('[data-edit-open="' + modalKey + '"]');
		var closeButtons = modal.querySelectorAll("[data-edit-close]");

		// Opens edit modal, clears stale state, and applies selected row data into form fields.
		function openModal(button) {
			clearErrors(form, summary);
			form.reset();
			applyButtonDataToForm(form, button);

			modal.classList.remove("is-hidden");
			modal.setAttribute("aria-hidden", "false");

			var firstInput = form.querySelector("input:not([type='hidden']), select, textarea");
			if (firstInput) {
				firstInput.focus();
			}
		}

		// Closes edit modal and resets validation/form state.
		function closeModal() {
			modal.classList.add("is-hidden");
			modal.setAttribute("aria-hidden", "true");
			clearErrors(form, summary);
			form.reset();
		}

		openButtons.forEach(function (button) {
			button.addEventListener("click", function () {
				openModal(button);
			});
		});

		closeButtons.forEach(function (button) {
			button.addEventListener("click", closeModal);
		});

		modal.addEventListener("click", function (event) {
			if (event.target && event.target.hasAttribute("data-edit-close")) {
				closeModal();
			}
		});

		form.querySelectorAll("input, select, textarea").forEach(function (input) {
			input.addEventListener("blur", function () {
				setFieldError(form, input.name, "");
				validateField(form, input);
			});
			input.addEventListener("change", function () {
				setFieldError(form, input.name, "");
				validateField(form, input);
			});
		});

		form.addEventListener("submit", function (event) {
			event.preventDefault();
			clearErrors(form, summary);

			if (!validateForm(form)) {
				return;
			}

			var formData = new FormData(form);
			fetch(form.action, {
				method: "POST",
				body: formData,
				headers: {
					"X-Requested-With": "XMLHttpRequest"
				}
			})
				.then(function (response) {
					return response.json().then(function (payload) {
						return { ok: response.ok, payload: payload };
					});
				})
				.then(function (result) {
					if (!result.ok) {
						if (result.payload && result.payload.errors) {
							Object.keys(result.payload.errors).forEach(function (key) {
								var messages = result.payload.errors[key];
								if (messages && messages.length > 0) {
									setFieldError(form, key, messages[0]);
								}
							});
						} else if (summary) {
							summary.textContent = "Unable to update record right now.";
						}
						return;
					}

					closeModal();
					window.location.reload();
				})
				.catch(function () {
					if (summary) {
						summary.textContent = "Unable to update record right now.";
					}
				});
		});
	});

	// Reopens an edit modal from URL query params (editType/editId), then cleans the URL.
	(function openEditModalFromQuery() {
		var params = new URLSearchParams(window.location.search);
		var editType = (params.get("editType") || "").toLowerCase();
		var editId = params.get("editId");
		if (!editType || !editId) {
			return;
		}

		var button = document.querySelector('[data-edit-open="' + editType + '"][data-edit-id="' + editId + '"]');
		if (!button) {
			return;
		}

		button.click();

		params.delete("editType");
		params.delete("editId");
		var cleanQuery = params.toString();
		var cleanUrl = window.location.pathname + (cleanQuery ? "?" + cleanQuery : "") + window.location.hash;
		window.history.replaceState({}, document.title, cleanUrl);
	})();
})();
