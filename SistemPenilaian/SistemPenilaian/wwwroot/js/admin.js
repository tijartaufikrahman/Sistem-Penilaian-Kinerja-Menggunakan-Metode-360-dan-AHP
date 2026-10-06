/* =======================================================
   Sistem Penilaian Admin JavaScript Core (Identik Project 7)
   100% jQuery & Adheres to aturan_request_jquery.txt
   ======================================================= */

// 1. Sidebar Controller
window.setSidebarState = function (open) {
    var $sidebar = $('#sidebar');
    var $mainContent = $('#mainContent');
    var $overlay = $('#sidebarOverlay');
    var $topbar = $('.topbar');
    var isMobile = $(window).width() <= 991;

    if (isMobile) {
        if (open) {
            $sidebar.removeClass("closed");
            $overlay.addClass("show");
        } else {
            $sidebar.addClass("closed");
            $overlay.removeClass("show");
        }
    } else {
        if (open) {
            $sidebar.removeClass("closed");
            $mainContent.removeClass("sidebar-closed");
            $topbar.removeClass("sidebar-closed").css("left", "");
        } else {
            $sidebar.addClass("closed");
            $mainContent.addClass("sidebar-closed");
            $topbar.addClass("sidebar-closed").css("left", "0");
        }
    }
};

window.toggleSidebar = function (explicitState) {
    var $sidebar = $('#sidebar');
    var isOpen = !$sidebar.hasClass("closed");
    if (typeof explicitState === 'boolean') {
        window.setSidebarState(explicitState);
    } else {
        window.setSidebarState(!isOpen);
    }
};

$(function () {
    var isMobile = $(window).width() <= 991;
    if (isMobile) {
        window.setSidebarState(false);
    }

    $(window).on("resize", function () {
        if ($(window).width() > 991) {
            window.setSidebarState(true);
            $('#sidebarOverlay').removeClass("show");
        } else {
            window.setSidebarState(false);
        }
    });
});

// 2. Toast Notification Controller (Pure jQuery SaaS Toast)
window.AdminToast = {
    _getContainer: function () {
        var $container = $('.admin-toast-wrap');
        if (!$container.length) {
            $container = $('<div class="admin-toast-wrap"></div>');
            $('body').append($container);
        }
        return $container;
    },
    show: function (msg, type) {
        var $container = this._getContainer();
        var icon = (type === 'error')
            ? '<i class="fas fa-circle-exclamation text-danger me-2"></i>'
            : '<i class="fas fa-circle-check text-teal me-2"></i>';
        var $item = $('<div class="admin-toast-item toast-' + (type || 'success') + '">' + icon + '<span>' + msg + '</span></div>');
        $container.append($item);

        setTimeout(function () {
            $item.css({
                opacity: '0',
                transform: 'translateY(-10px)',
                transition: 'all 0.2s'
            });
            setTimeout(function () { $item.remove(); }, 200);
        }, 2800);
    },
    success: function (msg) { this.show(msg, 'success'); },
    error: function (msg) { this.show(msg, 'error'); }
};

// 3. Modal Helper (Pure jQuery SaaS Modal)
window.AdminModal = {
    open: function (id) {
        $('#' + id).addClass('show');
    },
    close: function (id) {
        $('#' + id).removeClass('show');
    }
};

// 4. Global escape utility
window.escHtml = function (str) {
    if (!str && str !== 0) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
};

// Global format currency / number
window.formatNumberIndo = function (num, decimals) {
    if (num === null || num === undefined) return '-';
    var d = decimals !== undefined ? decimals : 2;
    return Number(num).toLocaleString('id-ID', { minimumFractionDigits: d, maximumFractionDigits: d });
};
