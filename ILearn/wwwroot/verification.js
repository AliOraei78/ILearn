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
