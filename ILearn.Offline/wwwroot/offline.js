document.getElementById('offline-reload')?.addEventListener('click', () => location.reload());
window.ilearnOffline = (() => {
    let database;
    const open = () => database ||= new Promise((resolve, reject) => {
        const request = indexedDB.open('ILearnOffline', 1);
        request.onupgradeneeded = () => request.result.createObjectStore('state');
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => { database = undefined; reject(new Error('فضای ذخیرهٔ دستگاه باز نشد.')); };
    });
    async function transaction(mode, action) {
        const db = await open();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('state', mode);
            let result, failure;
            tx.oncomplete = () => resolve(result);
            tx.onabort = tx.onerror = () => reject(failure || new Error('ذخیره انجام نشد؛ فضای دستگاه را بررسی کن.'));
            action(tx.objectStore('state'), value => result = value, message => { failure = new Error(message); tx.abort(); });
        });
    }
    const read = key => transaction('readonly', (store, done) => {
        const request = store.get(key);
        request.onsuccess = () => done(request.result ?? null);
    });
    const write = (key, value) => transaction('readwrite', store => store.put(value, key));
    const saveProgress = json => transaction('readwrite', (store, done, abort) => {
        const progress = JSON.parse(json);
        const request = store.get('progress');
        request.onsuccess = () => {
            const current = request.result ? JSON.parse(request.result) : { Revision: 0 };
            if (progress.Revision !== current.Revision) {
                abort('سیو در صفحهٔ دیگری تغییر کرده؛ این صفحه را دوباره باز کن. نوشتهٔ ذخیره‌نشده را اول کپی کن.');
                return;
            }
            progress.Revision++;
            store.put(JSON.stringify(progress), 'progress');
            done(progress.Revision);
        };
    });
    const snapshot = () => transaction('readonly', (store, done) => {
        const profile = store.get('profile'), progress = store.get('progress');
        progress.onsuccess = () => done(JSON.stringify({
            Format: 'ILearn.Offline', Version: 1,
            Profile: profile.result ? JSON.parse(profile.result) : { Id: '11111111-1111-4111-8111-111111111111', Name: 'بازیکن', Avatar: 'avatar-01', Stamp: '' },
            Progress: progress.result ? JSON.parse(progress.result) : { Version: 1, Revision: 0, Lesson: 'R001', Position: 0, Positions: {}, CodeRoomExercise: '', InterviewQuestion: '', Entries: {}, IntroductionsRead: [] }
        }));
    });
    function validateBackup(json) {
        const invalid = () => { throw new Error('فایل پشتیبان معتبر ILearn نیست؛ سیو قبلی تغییر نکرد.'); };
        if (typeof json !== 'string' || json.length > 32 * 1024 * 1024) invalid();
        let backup;
        try { backup = JSON.parse(json); } catch { invalid(); }
        const object = x => x && typeof x === 'object' && !Array.isArray(x);
        const text = (x, max = 100000) => typeof x === 'string' && x.length <= max;
        const integer = x => Number.isSafeInteger(x) && Math.abs(x) <= 2147483647;
        const list = (x, test, max = 2000) => Array.isArray(x) && x.length <= max && x.every(test);
        const map = (x, test, max = 5000) => object(x) && Object.keys(x).length <= max && Object.entries(x).every(([key, value]) => text(key, 300) && test(value));
        if (!object(backup) || backup.Format !== 'ILearn.Offline' || backup.Version !== 1) invalid();
        const p = backup.Profile, s = backup.Progress;
        if (!object(p) || !text(p.Name, 50) || !p.Name.trim() || !/^avatar-(0[1-9]|[1-3][0-9]|4[0-8])$/.test(p.Avatar) ||
            !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(p.Id) || !text(p.Stamp)) invalid();
        if (!object(s) || s.Version !== 1 || !integer(s.Revision) || s.Revision < 0 || !text(s.Lesson, 20) ||
            !integer(s.Position) || !map(s.Positions, integer) || !text(s.CodeRoomExercise, 300) || !text(s.InterviewQuestion, 300) || !list(s.IntroductionsRead, x => text(x, 100))) invalid();
        const game = g => object(g) && list(g.History, integer) && map(g.Marks, integer) && list(g.Trials, row => list(row, integer)) &&
            list(g.Undo, row => list(row, x => text(x, 300))) && list(g.PathUndo, row => list(row, integer)) && integer(g.Found) && integer(g.Removed);
        if (!map(s.Entries, e => object(e) && integer(e.Step) && game(e.GameState) && text(e.Draft, 16000) && text(e.FullAnswer) && text(e.Feedback) && text(e.Source, 300) &&
            list(e.Answers, x => text(x, 300)) && ['Complete', 'NeedsReview', 'FeedbackError'].every(key => typeof e[key] === 'boolean'))) invalid();
        return backup;
    }
    async function exportBackup() {
        const json = await snapshot();
        if (window.ILearnAndroid) { window.ILearnAndroid.saveBackup(json); return; }
        const url = URL.createObjectURL(new Blob([json], { type: 'application/json' }));
        const link = document.createElement('a');
        link.href = url; link.download = 'ILearn-backup.json'; link.click();
        setTimeout(() => URL.revokeObjectURL(url), 10000);
    }
    async function restoreBackup(json) {
        const backup = validateBackup(json);
        await transaction('readwrite', store => {
            const request = store.get('progress');
            request.onsuccess = () => {
                backup.Progress.Revision = (request.result ? JSON.parse(request.result).Revision : 0) + 1;
                store.put(JSON.stringify(backup.Profile), 'profile');
                store.put(JSON.stringify(backup.Progress), 'progress');
            };
        });
    }
    const apiKey = () => window.ILearnAndroid ? window.ILearnAndroid.getApiKey() : sessionStorage.getItem('ilearn-api-key') || '';
    function setApiKey(value) {
        value = value.trim();
        if (value && (!/^sk-or-[A-Za-z0-9_-]+$/.test(value) || value.length < 20 || value.length > 256)) throw new Error('کلید OpenRouter معتبر نیست.');
        if (window.ILearnAndroid) {
            if (!window.ILearnAndroid.setApiKey(value)) throw new Error('کلید در دستگاه ذخیره نشد؛ دوباره امتحان کن.');
        } else if (value) sessionStorage.setItem('ilearn-api-key', value);
        else sessionStorage.removeItem('ilearn-api-key');
    }
    return { read, write, saveProgress, exportBackup, restoreBackup, apiKey, setApiKey, hasApiKey: () => !!apiKey(), validateBackup: json => { validateBackup(json); } };
})();
