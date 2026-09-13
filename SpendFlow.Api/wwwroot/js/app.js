"use strict";

const expenseForm = document.getElementById("expense-form");
const expensesList = document.getElementById("expenses-list");
const errorMessage = document.getElementById("error-message");
const statusMessage = document.getElementById("status-message");
const descriptionInput = document.getElementById("description");
const amountInput = document.getElementById("amount");
const dateInput = document.getElementById("date");
const categoryInput = document.getElementById("category");

const categoryNames = {
    Food: "Еда",
    Transport: "Транспорт",
    Housing: "Жильё",
    Entertainment: "Развлечения",
    Health: "Здоровье",
    Other: "Другое"
};

let busy = false;

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

    // DELETE returns 204 with no JSON body.
    if (response.status === 204) {
        return null;
    }
    return response.json();
}

function renderExpenses(expenses) {
    expensesList.replaceChildren();
    if (expenses.length === 0) {
        showTableMessage("Расходов пока нет");
        return;
    }

    for (const expense of expenses) {
        const row = document.createElement("tr");
        // Date is a calendar date: format it without time zone conversion.
        const values = [
            expense.date.split("-").reverse().join("."),
            expense.description,
            categoryNames[expense.category] ?? expense.category,
            String(expense.amount).replace(".", ",")
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
        const expenses = await requestApi("/api/expenses");
        renderExpenses(expenses);
    } catch (error) {
        showTableMessage("Не удалось загрузить расходы");
        showError(error.message);
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
    if (!Number.isFinite(amount) || amount <= 0) {
        showError("Сумма должна быть больше нуля.");
        amountInput.focus();
        return;
    }

    const request = {
        description,
        amount,
        date: dateInput.value,
        category: categoryInput.value
    };

    setBusy(true);
    try {
        await requestApi("/api/expenses", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request)
        });
        expenseForm.reset();
        showStatus("Расход добавлен.");
        await loadExpenses();
    } catch (error) {
        showError(error.message);
    } finally {
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
        await loadExpenses();
    } catch (error) {
        showError(error.message);
    } finally {
        setBusy(false);
    }
}

expenseForm.addEventListener("submit", addExpense);
setBusy(true);
loadExpenses().finally(() => setBusy(false));
