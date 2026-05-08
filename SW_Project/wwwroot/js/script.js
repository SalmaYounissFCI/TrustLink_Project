

// ── NAVBAR SCROLL BEHAVIOR ──
(function () {
    const navbar = document.querySelector('.tl-navbar');
    if (!navbar) return;
    const onScroll = () => {
        navbar.classList.toggle('scrolled', window.scrollY > 40);
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
})();

// ── INTERSECTION OBSERVER: FADE IN ──
(function () {
    const els = document.querySelectorAll('.fade-in-up');
    if (!els.length) return;
    const io = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                e.target.classList.add('visible');
                io.unobserve(e.target);
            }
        });
    }, { threshold: 0.12 });
    els.forEach(el => io.observe(el));
})();

// ── TOAST UTILITY ──
function showToast(msg, icon = 'bi-check-circle-fill') {
    const existing = document.querySelector('.tl-toast');
    if (existing) existing.remove();
    const t = document.createElement('div');
    t.className = 'tl-toast';
    t.innerHTML = `<i class="bi ${icon}"></i><span>${msg}</span>`;
    document.body.appendChild(t);
    requestAnimationFrame(() => {
        requestAnimationFrame(() => t.classList.add('show'));
    });
    setTimeout(() => {
        t.classList.remove('show');
        setTimeout(() => t.remove(), 400);
    }, 3500);
}

// ── FAVORITE TOGGLE ──
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.listing-card-favorite');
    if (!btn) return;
    btn.classList.toggle('active');
    const icon = btn.querySelector('i');
    if (btn.classList.contains('active')) {
        icon.className = 'bi bi-heart-fill';
        showToast('Listing saved to your favorites.');
    } else {
        icon.className = 'bi bi-heart';
        showToast('Listing removed from favorites.', 'bi-x-circle');
    }
});

// ── COUNTER ANIMATION ──
function animateCounter(el, target, duration = 1800) {
    let start = 0;
    const step = (timestamp) => {
        if (!start) start = timestamp;
        const progress = Math.min((timestamp - start) / duration, 1);
        const ease = 1 - Math.pow(1 - progress, 3);
        const current = Math.floor(ease * target);
        el.textContent = current.toLocaleString() + (el.dataset.suffix || '');
        if (progress < 1) requestAnimationFrame(step);
        else el.textContent = target.toLocaleString() + (el.dataset.suffix || '');
    };
    requestAnimationFrame(step);
}

(function () {
    const counters = document.querySelectorAll('[data-counter]');
    if (!counters.length) return;
    const io = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                const el = e.target;
                animateCounter(el, parseInt(el.dataset.counter, 10));
                io.unobserve(el);
            }
        });
    }, { threshold: 0.5 });
    counters.forEach(c => io.observe(c));
})();

// ── CATEGORY PILLS FILTER ──
(function () {
    const pills = document.querySelectorAll('.category-pill');
    if (!pills.length) return;
    pills.forEach(pill => {
        pill.addEventListener('click', function () {
            pills.forEach(p => p.classList.remove('active'));
            this.classList.add('active');
            const category = this.dataset.category || 'all';
            filterListings(category);
        });
    });

    function filterListings(cat) {
        const cards = document.querySelectorAll('[data-category]');
        cards.forEach(card => {
            const parent = card.closest('.listing-col');
            if (!parent) return;
            if (cat === 'all' || card.dataset.category === cat) {
                parent.style.display = '';
                card.style.opacity = '0';
                card.style.transform = 'translateY(20px)';
                requestAnimationFrame(() => {
                    card.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
                    card.style.opacity = '1';
                    card.style.transform = 'translateY(0)';
                });
            } else {
                parent.style.display = 'none';
            }
        });
        const visible = [...document.querySelectorAll('.listing-col')].filter(c => c.style.display !== 'none');
        const countEl = document.querySelector('.listings-count strong');
        if (countEl) countEl.textContent = visible.length;
    }
})();

// ── VIEW TOGGLE (GRID / LIST) ──
(function () {
    const toggleBtns = document.querySelectorAll('.view-toggle-btn');
    const grid = document.querySelector('.listings-grid');
    if (!toggleBtns.length || !grid) return;
    toggleBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            toggleBtns.forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            if (this.dataset.view === 'list') {
                grid.classList.add('list-view');
            } else {
                grid.classList.remove('list-view');
            }
        });
    });
})();

// ── SEARCH BAR ──
(function () {
    const searchInput = document.querySelector('.search-bar-input');
    const searchBtn = document.querySelector('.search-bar-btn');
    if (!searchInput) return;

    function doSearch() {
        const q = searchInput.value.trim().toLowerCase();
        const cards = document.querySelectorAll('.listing-card');
        let count = 0;
        cards.forEach(card => {
            const col = card.closest('.listing-col');
            const title = card.querySelector('.listing-card-title');
            const loc = card.querySelector('.listing-card-location');
            const text = ((title?.textContent || '') + ' ' + (loc?.textContent || '')).toLowerCase();
            const show = !q || text.includes(q);
            if (col) col.style.display = show ? '' : 'none';
            if (show) count++;
        });
        const countEl = document.querySelector('.listings-count strong');
        if (countEl) countEl.textContent = count;
    }

    if (searchBtn) searchBtn.addEventListener('click', doSearch);
    searchInput.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(); });
})();

// ── SORT SELECT ──
(function () {
    const sel = document.querySelector('.sort-select');
    if (!sel) return;
    sel.addEventListener('change', function () {
        showToast('Listings sorted by ' + this.options[this.selectedIndex].text.toLowerCase() + '.');
    });
})();

// ── FILTER CLEAR ──
document.addEventListener('click', function (e) {
    if (!e.target.classList.contains('filter-clear')) return;
    document.querySelectorAll('.filter-check input[type="checkbox"]').forEach(cb => cb.checked = false);
    document.querySelectorAll('.price-input-field').forEach(f => f.value = '');
    showToast('Filters cleared.');
});

// ── SMOOTH ANCHOR SCROLL ──
document.addEventListener('click', function (e) {
    const link = e.target.closest('a[href^="#"]');
    if (!link) return;
    const id = link.getAttribute('href').slice(1);
    const target = document.getElementById(id);
    if (!target) return;
    e.preventDefault();
    const offset = 80;
    const y = target.getBoundingClientRect().top + window.scrollY - offset;
    window.scrollTo({ top: y, behavior: 'smooth' });
});

// ── MOBILE NAVBAR CLOSE ON LINK ──
(function () {
    const toggler = document.querySelector('.navbar-toggler');
    const navLinks = document.querySelectorAll('.tl-navbar .nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', () => {
            const collapse = document.querySelector('.navbar-collapse.show');
            if (collapse && toggler) toggler.click();
        });
    });
})();

// ── HIW: ANIMATE STEPS ──
(function () {
    const steps = document.querySelectorAll('.hiw-step');
    if (!steps.length) return;
    const io = new IntersectionObserver((entries) => {
        entries.forEach((e, i) => {
            if (e.isIntersecting) {
                setTimeout(() => {
                    e.target.style.opacity = '1';
                    e.target.style.transform = 'translateX(0)';
                }, i * 80);
                io.unobserve(e.target);
            }
        });
    }, { threshold: 0.1 });
    steps.forEach(step => {
        step.style.opacity = '0';
        step.style.transform = 'translateX(-20px)';
        step.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
        io.observe(step);
    });
})();

// ── BACK TO TOP ──
(function () {
    const btn = document.getElementById('back-to-top');
    if (!btn) return;
    window.addEventListener('scroll', () => {
        btn.style.opacity = window.scrollY > 400 ? '1' : '0';
        btn.style.pointerEvents = window.scrollY > 400 ? 'auto' : 'none';
    }, { passive: true });
    btn.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
})();
function updateNotificationCount() {
    fetch('/Notifications/GetUnreadCount')
        .then(res => res.json())
        .then(count => {
            const badge = document.getElementById('notif-badge');
            const dot = document.getElementById('notif-dot');
            const bell = document.getElementById('notificationBell');

            if (badge) {
                if (count > 0) {
                    badge.textContent = count > 99 ? '99+' : count;
                    badge.style.display = 'flex';

                    // ✅ إظهار النقطة الحمراء
                    if (dot) dot.style.display = 'block';

                    // ✅ إضافة تأثير التوهج والاهتزاز للأيقونة
                    if (bell && !bell.classList.contains('has-notif')) {
                        bell.classList.add('has-notif');

                        // إزالة الاهتزاز بعد 2 ثانية (يبقى التوهج)
                        setTimeout(() => {
                            bell.style.animation = 'none';
                            setTimeout(() => {
                                bell.style.animation = '';
                            }, 100);
                        }, 2000);
                    }
                } else {
                    badge.style.display = 'none';
                    if (dot) dot.style.display = 'none';
                    if (bell) bell.classList.remove('has-notif');
                }
            }
        });
}

// تحديث عند تحميل الصفحة
updateNotificationCount();

// تحديث كل 30 ثانية
setInterval(updateNotificationCount, 30000);
// عند النقر على الجرس، أزل تأثير التوهج مؤقتاً
document.getElementById('notificationBell')?.addEventListener('click', function () {
    const dot = document.getElementById('notif-dot');
    const bell = document.getElementById('notificationBell');
    if (dot) dot.style.display = 'none';
    if (bell) bell.classList.remove('has-notif');
});