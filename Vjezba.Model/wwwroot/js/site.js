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
		document.documentElement.classList.toggle("analog-sidebar-collapsed", collapsed);
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

(function () {
	var modals = document.querySelectorAll("[data-create-modal]");
	if (!modals.length) {
		return;
	}

	function isEmail(value) {
		return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
	}

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

	function generateTrackingNumber() {
		var value = Math.floor(Math.random() * 1000000).toString().padStart(6, "0");
		return "TN-" + value;
	}

	function formatLocalDateTime(value) {
		function pad(number) {
			return number.toString().padStart(2, "0");
		}

		return value.getFullYear() + "-" +
			pad(value.getMonth() + 1) + "-" +
			pad(value.getDate()) + "T" +
			pad(value.getHours()) + ":" +
			pad(value.getMinutes());
	}

	function isEmptyDateValue(value) {
		return !value || value.indexOf("0001-01-01") === 0;
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
					departureInput.value = formatLocalDateTime(departureBase);
				}
				var departureValue = departureInput && !isEmptyDateValue(departureInput.value)
					? new Date(departureInput.value)
					: departureBase;
				if (arrivalInput && isEmptyDateValue(arrivalInput.value)) {
					var arrivalValue = new Date(departureValue.getTime());
					arrivalValue.setDate(arrivalValue.getDate() + 7);
					arrivalInput.value = formatLocalDateTime(arrivalValue);
				}
			}
			if (modalKey === "statuslog") {
				var timeInput = form.querySelector("[name='TimeChanged']");
				if (timeInput && isEmptyDateValue(timeInput.value)) {
					timeInput.value = formatLocalDateTime(new Date());
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
