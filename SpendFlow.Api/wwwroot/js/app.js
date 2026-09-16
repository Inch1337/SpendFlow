"use strict";

const expenseForm = document.getElementById("expense-form");
const expensesList = document.getElementById("expenses-list");
const errorMessage = document.getElementById("error-message");
const statusMessage = document.getElementById("status-message");
const descriptionInput = document.getElementById("description");
const amountInput = document.getElementById("amount");
const dateInput = document.getElementById("date");
const categoryInput = document.getElementById("category");
const filtersForm = document.getElementById("filters-form");
const filterDateFrom = document.getElementById("filter-date-from");
const filterDateTo = document.getElementById("filter-date-to");
const filterCategory = document.getElementById("filter-category");
const filterSearch = document.getElementById("filter-search");
const resetFiltersButton = document.getElementById("reset-filters-button");
const currentMonthButton = document.getElementById("current-month-button");
const summaryPeriod = document.getElementById("summary-period");
const summaryMessage = document.getElementById("summary-message");
const summaryContent = document.getElementById("summary-content");
const summaryTotal = document.getElementById("summary-total");
const categoryChart = document.getElementById("category-chart");

const categoryNames = {
    Food: "Еда",
    Transport: "Транспорт",
    Housing: "Жильё",
    Entertainment: "Развлечения",
    Health: "Здоровье",
    Other: "Другое"
};

let busy = false;

let appliedFilters = {};

function setBusy(value) {
    busy = value;
    document.querySelectorAll("button").forEach(button => {
        button.disabled = value;
    });
}

function showError(message) {
    errorMessage.textContent = message;
    errorMessage.hidden = !message;
}

function showStatus(message) {
    statusMessage.textContent = message;
    statusMessage.hidden = !message;
}

function showTableMessage(message) {
    const row = document.createElement("tr");
    const cell = document.createElement("td");
    cell.colSpan = 5;
    cell.className = "empty-message";
    cell.textContent = message;
    row.append(cell);
    expensesList.replaceChildren(row);
}

async function requestApi(url, options = {}) {
    let response;
    try {
        response = await fetch(url, options);
    } catch {
        throw new Error("Не удалось связаться с сервером. Проверьте подключение.");
    }

    if (!response.ok) {
        if (response.status === 400) {
            const problem = await response.json().catch(() => null);
            const messages = problem?.errors ? Object.values(problem.errors).flat() : [];
            throw new Error(messages.length ? messages.join(" ") : "Проверьте введённые данные.");
        }
        if (response.status === 404) {
            throw new Error("Расход не найден. Возможно, он уже удалён.");
        }
        throw new Error("Не удалось выполнить запрос. Попробуйте позже.");
    }


    if (response.status === 204) {
        return null;
    }
    return response.json();
}

function formatAmount(amount) {
    return amount.toFixed(2).replace(".", ",");
}

const calendarDateFormatter = new Intl.DateTimeFormat(undefined, {
    year: "numeric", month: "2-digit", day: "2-digit", timeZone: "UTC"
});

function formatCalendarDate(value) {
    const [year, month, day] = value.split("-").map(Number);
    // UTC is used only for display, preserving the calendar day in every user time zone.
    const date = new Date(0);
    date.setUTCFullYear(year, month - 1, day);
    return calendarDateFormatter.format(date);
}

function setExpenseFormDisabled(value) {
    expenseForm.querySelectorAll("input, select").forEach(field => {
        field.disabled = value;
    });
}

function isValidAmount(value) {
    const amount = Number(value);
    if (!Number.isFinite(amount) || amount < 0.01 || amount > 999999999.99) {
        return false;
    }

    const match = /^(\d+\.?\d*|\.\d+)(?:e([+-]?\d+))?$/i.exec(value);
    if (!match) return false;


    const fraction = match[1].split(".")[1] ?? "";
    const trailingZeros = match[1].replace(".", "").match(/0*$/)[0].length;
    const decimalPlaces = fraction.length - Number(match[2] ?? 0) - trailingZeros;
    return decimalPlaces <= 2;
}

function renderExpenses(expenses) {
    expensesList.replaceChildren();
    if (expenses.length === 0) {
        showTableMessage("Расходы не найдены");
        return;
    }

    for (const expense of expenses) {
        const row = document.createElement("tr");

        const values = [
            formatCalendarDate(expense.date),
            expense.description,
            categoryNames[expense.category] ?? expense.category,
            formatAmount(expense.amount)
        ];

        values.forEach((value, index) => {
            const cell = document.createElement("td");
            cell.textContent = value;
            if (index === 3) {
                cell.className = "amount";
            }
            row.append(cell);
        });

        const actions = document.createElement("td");
        const deleteButton = document.createElement("button");
        deleteButton.type = "button";
        deleteButton.textContent = "Удалить";
        deleteButton.disabled = busy;
        deleteButton.addEventListener("click", () => deleteExpense(expense.id));
        actions.append(deleteButton);
        row.append(actions);
        expensesList.append(row);
    }
}

async function loadExpenses() {
    showTableMessage("Загрузка расходов…");
    try {
        const parameters = new URLSearchParams();
        for (const [name, value] of Object.entries(appliedFilters)) {
            if (value) {
                parameters.set(name, value);
            }
        }
        const query = parameters.toString();
        const expenses = await requestApi(query ? `/api/expenses?${query}` : "/api/expenses");
        renderExpenses(expenses);
    } catch (error) {
        showTableMessage("Не удалось загрузить расходы");
        showError(error.message);
    }
}

function renderSummary(summary) {
    summaryTotal.textContent = formatAmount(summary.totalAmount);
    categoryChart.replaceChildren();
    const maximum = Math.max(0, ...summary.byCategory.map(item => item.totalAmount));

    for (const item of summary.byCategory) {
        const row = document.createElement("li");
        const label = document.createElement("div");
        label.className = "category-label";
        const name = document.createElement("span");
        name.textContent = categoryNames[item.category] ?? item.category;
        const amount = document.createElement("span");
        amount.textContent = formatAmount(item.totalAmount);
        label.append(name, amount);


        const track = document.createElement("div");
        track.className = "bar-track";
        track.setAttribute("aria-hidden", "true");
        const fill = document.createElement("div");
        fill.className = "bar-fill";
        fill.style.width = `${maximum > 0 ? item.totalAmount / maximum * 100 : 0}%`;
        track.append(fill);
        row.append(label, track);
        categoryChart.append(row);
    }

    summaryMessage.textContent = summary.totalAmount === 0 ? "За этот период расходов нет." : "";
    summaryMessage.hidden = summary.totalAmount !== 0;
    summaryContent.hidden = false;
}

async function loadSummary() {
    summaryContent.hidden = true;
    summaryMessage.hidden = false;
    summaryMessage.textContent = "Загрузка статистики…";

    const { dateFrom, dateTo } = appliedFilters;
    const from = dateFrom ? formatCalendarDate(dateFrom) : "";
    const to = dateTo ? formatCalendarDate(dateTo) : "";
    summaryPeriod.textContent = from && to ? `Период: ${from} - ${to}`
        : from ? `Начиная с ${from}` : to ? `По ${to} включительно` : "За всё время";

    const parameters = new URLSearchParams();
    if (dateFrom) parameters.set("dateFrom", dateFrom);
    if (dateTo) parameters.set("dateTo", dateTo);
    const query = parameters.toString();

    try {
        const summary = await requestApi(query ? `/api/expenses/summary?${query}` : "/api/expenses/summary");
        renderSummary(summary);
    } catch (error) {
        summaryMessage.textContent = `Не удалось загрузить статистику. ${error.message}`;
    }
}

async function refreshData() {
    await Promise.all([loadExpenses(), loadSummary()]);
}

function applyCurrentMonth() {
    if (busy) {
        return;
    }

    const today = new Date();
    const year = today.getFullYear();
    const month = today.getMonth();
    const prefix = `${year}-${String(month + 1).padStart(2, "0")}`;
    const lastDay = new Date(year, month + 1, 0).getDate();
    filterDateFrom.value = `${prefix}-01`;
    filterDateTo.value = `${prefix}-${lastDay}`;
    filtersForm.requestSubmit();
}

async function applyFilters(event) {
    event.preventDefault();
    if (busy) {
        return;
    }

    showError("");
    showStatus("");
    const dateFrom = filterDateFrom.value;
    const dateTo = filterDateTo.value;

    if (dateFrom && dateTo && dateFrom > dateTo) {
        showError("Дата начала периода не должна быть позже даты окончания.");
        filterDateFrom.focus();
        return;
    }

    appliedFilters = {
        dateFrom,
        dateTo,
        category: filterCategory.value,
        search: filterSearch.value.trim()
    };

    setBusy(true);
    try {
        await refreshData();
    } finally {
        setBusy(false);
    }
}

async function resetFilters() {
    if (busy) {
        return;
    }

    filtersForm.reset();
    appliedFilters = {};
    showError("");
    showStatus("");
    setBusy(true);
    try {
        await refreshData();
    } finally {
        setBusy(false);
    }
}

async function addExpense(event) {
    event.preventDefault();
    if (busy) {
        return;
    }

    showError("");
    showStatus("");
    const description = descriptionInput.value.trim();
    const amount = amountInput.valueAsNumber;
    if (!description) {
        showError("Описание не должно состоять только из пробелов.");
        descriptionInput.focus();
        return;
    }
    if (!isValidAmount(amountInput.value)) {
        showError("Сумма должна быть от 0,01 до 999999999,99 и содержать не более двух знаков после запятой.");
        amountInput.focus();
        return;
    }

    const date = dateInput.value;

    const request = {
        description,
        amount,
        date,
        category: categoryInput.value
    };

    setBusy(true);
    setExpenseFormDisabled(true);
    try {
        await requestApi("/api/expenses", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request)
        });
        expenseForm.reset();
        showStatus("Расход добавлен.");
        await refreshData();
    } catch (error) {
        showError(error.message);
    } finally {
        setExpenseFormDisabled(false);
        setBusy(false);
    }
}

async function deleteExpense(id) {
    if (busy || !window.confirm("Удалить этот расход?")) {
        return;
    }

    showError("");
    showStatus("");
    setBusy(true);
    try {
        await requestApi(`/api/expenses/${id}`, { method: "DELETE" });
        showStatus("Расход удалён.");
        await refreshData();
    } catch (error) {
        showError(error.message);
    } finally {
        setBusy(false);
    }
}

expenseForm.addEventListener("submit", addExpense);
filtersForm.addEventListener("submit", applyFilters);
resetFiltersButton.addEventListener("click", resetFilters);
currentMonthButton.addEventListener("click", applyCurrentMonth);
setBusy(true);
refreshData().finally(() => setBusy(false));
