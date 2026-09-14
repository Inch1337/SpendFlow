const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function loadApp() {
    const elements = new Map();
    const fields = ['description', 'amount', 'date', 'category'];
    const document = {
        getElementById(id) {
            if (!elements.has(id)) elements.set(id, {
                value: '', disabled: false, focus() {},
                querySelectorAll: () => fields.map(name => document.getElementById(name)),
                reset() { fields.forEach(name => document.getElementById(name).value = ''); }
            });
            return elements.get(id);
        },
        querySelectorAll: () => []
    };
    const app = vm.createContext({ document });
    const source = fs.readFileSync(path.join(__dirname, '../../SpendFlow.Api/wwwroot/js/app.js'), 'utf8');
    vm.runInContext(source.slice(0, source.indexOf('expenseForm.addEventListener')), app);
    app.refreshData = async () => {};
    return { app, elements, fields };
}

test('dates: exact format, leap years and DateOnly boundaries', () => {
    const { app } = loadApp();
    for (const [input, expected] of [
        ['15/01/2026', '2026-01-15'], ['29/02/2024', '2024-02-29'],
        ['29/02/2000', '2000-02-29'], ['01/01/0001', '0001-01-01'],
        ['31/12/9999', '9999-12-31']
    ]) {
        assert.equal(app.parseDate(input), expected);
        assert.equal(app.formatDate(expected), input);
    }
    for (const input of ['', '1/2/2026', '2026-01-15', '30/02/2026',
        '29/02/1900', '29/02/2025', '31/04/2026', '00/01/2026',
        '01/13/2026', '01/01/0000']) assert.equal(app.parseDate(input), null, input);
});

for (const succeeds of [true, false]) {
    test(`saving locks fields; ${succeeds ? 'success resets' : 'failure preserves'} input`, async () => {
        const { app, elements, fields } = loadApp();
        elements.get('description').value = 'Coffee';
        elements.get('amount').value = '12.30';
        elements.get('amount').valueAsNumber = 12.3;
        elements.get('date').value = '15/01/2026';
        elements.get('category').value = 'Food';
        let resolve, reject, body;
        app.requestApi = (url, options) => {
            body = JSON.parse(options.body);
            return new Promise((yes, no) => { resolve = yes; reject = no; });
        };
        const pending = app.addExpense({ preventDefault() {} });
        fields.forEach(name => assert.equal(elements.get(name).disabled, true, name));
        assert.equal(body.date, '2026-01-15');
        if (succeeds) resolve({}); else reject(new Error('Test error'));
        await pending;
        fields.forEach(name => assert.equal(elements.get(name).disabled, false, name));
        assert.equal(elements.get('description').value, succeeds ? '' : 'Coffee');
        assert.equal(elements.get('date').value, succeeds ? '' : '15/01/2026');
    });
}

test('filters convert dates before comparing, allow open periods, reject invalid dates', async () => {
    const { app, elements } = loadApp();
    let calls = 0;
    app.refreshData = async () => { calls++; };
    elements.get('filter-date-from').value = '31/01/2026';
    elements.get('filter-date-to').value = '01/02/2026';
    await app.applyFilters({ preventDefault() {} });
    assert.equal(calls, 1);
    assert.equal(vm.runInContext('appliedFilters.dateFrom', app), '2026-01-31');
    elements.get('filter-date-to').value = '01/01/2026';
    await app.applyFilters({ preventDefault() {} });
    assert.equal(calls, 1);
    elements.get('filter-date-to').value = '';
    await app.applyFilters({ preventDefault() {} });
    assert.equal(calls, 2);
    elements.get('filter-date-from').value = '30/02/2026';
    await app.applyFilters({ preventDefault() {} });
    assert.equal(calls, 2);
});

test('invalid expense date prevents the API call and keeps the form editable', async () => {
    const { app, elements } = loadApp();
    elements.get('description').value = 'Coffee';
    elements.get('amount').value = '12';
    elements.get('amount').valueAsNumber = 12;
    elements.get('date').value = '30/02/2026';
    app.requestApi = () => assert.fail('Invalid date must not reach API');
    await app.addExpense({ preventDefault() {} });
    assert.equal(elements.get('date').disabled, false);
    assert.equal(elements.get('date').value, '30/02/2026');
    assert.match(elements.get('error-message').textContent, /dd\/mm\/yyyy/);
});

test('current month fills formatted calendar boundaries and submits filters', () => {
    const { app, elements } = loadApp();
    vm.runInContext(`Date = class extends Date {
        constructor(...args) { super(...(args.length ? args : [2024, 1, 15])); }
    }`, app);
    let submitted = false;
    elements.get('filters-form').requestSubmit = () => { submitted = true; };
    app.applyCurrentMonth();
    assert.equal(elements.get('filter-date-from').value, '01/02/2024');
    assert.equal(elements.get('filter-date-to').value, '29/02/2024');
    assert.equal(submitted, true);
});
