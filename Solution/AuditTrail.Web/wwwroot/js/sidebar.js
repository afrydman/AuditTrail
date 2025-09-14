/* Enhanced Sidebar JavaScript - Professional Navigation */
/* This extends the existing sidebar functionality in custom.js */

$(document).ready(function() {
    'use strict';
    
    // Initialize enhanced sidebar features
    EnhancedSidebar.init();
});

/**
 * Enhanced Sidebar Module - Extends existing functionality
 */
var EnhancedSidebar = {
    
    /**
     * Initialize enhanced sidebar functionality
     */
    init: function() {
        this.addTooltips();
        this.restoreSidebarState();
        this.enhanceCurrentPageDetection();
        this.addAccessibilityFeatures();
        this.addKeyboardShortcuts();
        this.enhanceExistingFunctionality();
    },

    /**
     * Add tooltips to collapsed menu items
     */
    addTooltips: function() {
        $('.sidebar-menu ul > li').each(function() {
            var $item = $(this);
            var $link = $item.find('> a');
            var menuText = $link.find('.menu-text').text().trim();
            
            if (menuText) {
                $item.attr('data-tooltip', menuText);
            }
        });
    },

    /**
     * Restore sidebar state from localStorage
     */
    restoreSidebarState: function() {
        var savedState = localStorage.getItem('audittrail_sidebar_pinned');
        
        if (savedState === 'true') {
            if (!$('.page-wrapper').hasClass('pinned')) {
                $('#pin-sidebar').click();
            }
        }
    },

    /**
     * Save sidebar state to localStorage
     */
    saveSidebarState: function(isPinned) {
        localStorage.setItem('audittrail_sidebar_pinned', isPinned);
    },

    /**
     * Enhance current page detection
     */
    enhanceCurrentPageDetection: function() {
        var currentPath = window.location.pathname.toLowerCase();
        var $menuItems = $('.sidebar-menu ul > li');
        
        $menuItems.each(function() {
            var $item = $(this);
            var $link = $item.find('> a');
            var href = $link.attr('href');
            
            if (href) {
                var linkPath = href.split('?')[0].toLowerCase();
                if (currentPath.includes(linkPath) && linkPath.length > 1) {
                    $item.addClass('current-page');
                    
                    // Open parent dropdown if this is a submenu item
                    var $parentDropdown = $item.closest('.sidebar-dropdown');
                    if ($parentDropdown.length) {
                        $parentDropdown.addClass('active');
                    }
                }
            }
        });
    },

    /**
     * Add accessibility features
     */
    addAccessibilityFeatures: function() {
        // Add ARIA labels and roles
        $('.sidebar-wrapper').attr('role', 'navigation').attr('aria-label', 'Navegación principal');
        
        $('.sidebar-menu a').each(function() {
            var $link = $(this);
            var text = $link.find('.menu-text').text() || $link.text();
            if (text.trim()) {
                $link.attr('aria-label', text.trim());
            }
        });
    },

    /**
     * Add keyboard shortcuts
     */
    addKeyboardShortcuts: function() {
        var self = this;
        
        $(document).on('keydown', function(e) {
            // Ctrl + B to toggle sidebar pin
            if (e.ctrlKey && e.key === 'b') {
                e.preventDefault();
                $('#pin-sidebar').click();
            }
            
            // Escape to close mobile sidebar
            if (e.key === 'Escape') {
                if ($('.page-wrapper').hasClass('toggled')) {
                    $('#toggle-sidebar').click();
                }
            }
        });
    },

    /**
     * Enhance existing functionality with state persistence
     */
    enhanceExistingFunctionality: function() {
        var self = this;
        
        // Override the existing pin-sidebar click handler to add state persistence
        $('#pin-sidebar').off('click').on('click', function() {
            if ($('.page-wrapper').hasClass('pinned')) {
                // unpin sidebar when hovered
                $('.page-wrapper').removeClass('pinned');
                $('#sidebar').unbind('hover');
                self.saveSidebarState(false);
            } else {
                $('.page-wrapper').addClass('pinned');
                $('#sidebar').hover(
                    function() {
                        $('.page-wrapper').addClass('sidebar-hovered');
                    },
                    function() {
                        $('.page-wrapper').removeClass('sidebar-hovered');
                    }
                );
                self.saveSidebarState(true);
            }
        });

        // Add smooth loading animation for menu clicks
        $('.sidebar-menu a').on('click', function() {
            var $link = $(this);
            if (!$link.parent().hasClass('sidebar-dropdown')) {
                $link.addClass('loading');
                setTimeout(function() {
                    $link.removeClass('loading');
                }, 500);
            }
        });
    }
};

// Add CSS for loading states and enhanced animations
$(document).ready(function() {
    if (!$('#sidebar-enhanced-styles').length) {
        $('<style id="sidebar-enhanced-styles">')
            .prop('type', 'text/css')
            .html(`
                .sidebar-menu ul > li > a.loading {
                    pointer-events: none;
                    opacity: 0.7;
                }
                
                .sidebar-menu ul > li > a.loading::after {
                    content: '';
                    position: absolute;
                    top: 50%;
                    right: 15px;
                    width: 12px;
                    height: 12px;
                    border: 2px solid transparent;
                    border-top: 2px solid #0073d8;
                    border-radius: 50%;
                    animation: spin 1s linear infinite;
                    transform: translateY(-50%);
                }
                
                @keyframes spin {
                    0% { transform: translateY(-50%) rotate(0deg); }
                    100% { transform: translateY(-50%) rotate(360deg); }
                }
                
                .sidebar-menu ul > li > a:focus {
                    box-shadow: 0 0 0 2px rgba(0, 115, 216, 0.3);
                    outline: none;
                }
                
                /* Smooth transitions for all interactive elements */
                .sidebar-wrapper,
                .sidebar-menu > li > a,
                .sidebar-menu > li > a > i,
                .sidebar-submenu {
                    backface-visibility: hidden;
                    transform-style: preserve-3d;
                }
            `)
            .appendTo('head');
    }
});