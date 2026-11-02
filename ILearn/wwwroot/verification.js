document.querySelectorAll('[data-resend-seconds]').forEach(button => {
    const deadline = performance.now() + Number(button.dataset.resendSeconds) * 1000;
    const digits = new Intl.NumberFormat('fa-IR');
    const update = () => {
        const remaining = Math.max(0, Math.ceil((deadline - performance.now()) / 1000));
        button.disabled = remaining > 0;
        button.textContent = remaining ? `ارسال دوباره (${digits.format(remaining)} ثانیه)` : 'ارسال دوباره کد';
        return remaining;
    };
    if (update()) {
        const timer = setInterval(() => { if (!update()) clearInterval(timer); }, 1000);
    }
});

document.addEventListener('input', event => {
    if (!event.target.matches('input[autocomplete="new-password"]')) return;
    const fields = event.target.form?.querySelectorAll('input[autocomplete="new-password"]');
    if (fields?.length !== 2) return;
    const [password, confirmation] = fields;
    if (password.id === 'new-password') confirmation.required = password.value.length > 0;
    confirmation.setCustomValidity(confirmation.value && confirmation.value !== password.value ? 'تکرار رمز باید با رمز تازه یکسان باشد.' : '');
});
document.addEventListener('submit', event => {
    const form = event.target;
    if (!form.matches('form[action^="/auth/"]')) return;
    if (form.dataset.submitting) { event.preventDefault(); return; }
    form.dataset.submitting = 'true';
    form.setAttribute('aria-busy', 'true');
    const button = form.querySelector('button:not([type="button"])');
    if (button) { button.dataset.idleText = button.textContent; button.disabled = true; button.textContent = 'در حال انجام…'; }
});
window.addEventListener('pageshow', () => {
    document.querySelectorAll('form[data-submitting]').forEach(form => {
        delete form.dataset.submitting; form.removeAttribute('aria-busy');
        const button = form.querySelector('button[data-idle-text]');
        if (button) { button.disabled = false; button.textContent = button.dataset.idleText; delete button.dataset.idleText; }
    });
});
