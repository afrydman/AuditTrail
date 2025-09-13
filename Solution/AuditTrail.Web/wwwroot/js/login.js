/* Login Page JavaScript - Enhanced with jQuery */

$(document).ready(function() {
    'use strict';
    
    // Initialize all login page functionality
    LoginPage.init();
});

/**
 * Login Page Module - Handles all login page interactions
 */
var LoginPage = {
    
    // Configuration
    config: {
        selectors: {
            form: '.needs-validation',
            usernameField: '#username',
            passwordField: '#password',
            toggleButton: '#togglePassword',
            toggleIcon: '#toggleIcon',
            submitButton: '.btn-login',
            alertContainer: '.alert-danger'
        },
        classes: {
            validated: 'was-validated',
            loading: 'btn-loading',
            shake: 'shake-error'
        },
        messages: {
            loading: '<i class="bi bi-hourglass-split me-2"></i>Iniciando sesión...',
            originalButton: '<i class="bi bi-box-arrow-in-right me-2"></i>Iniciar Sesión'
        }
    },

    /**
     * Initialize login page functionality
     */
    init: function() {
        this.initializeFormValidation();
        this.initializePasswordToggle();
        this.initializeFormEnhancements();
        this.setUsernameFocus();
        this.bindEvents();
    },

    /**
     * Initialize Bootstrap form validation with jQuery
     */
    initializeFormValidation: function() {
        var self = this;
        
        $(this.config.selectors.form).on('submit', function(event) {
            var $form = $(this);
            
            if (!this.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
                self.showValidationErrors($form);
            } else {
                self.showLoadingState();
            }
            
            $form.addClass(self.config.classes.validated);
        });
    },

    /**
     * Initialize password visibility toggle with enhanced UX
     */
    initializePasswordToggle: function() {
        var self = this;
        var $toggleButton = $(this.config.selectors.toggleButton);
        var $passwordField = $(this.config.selectors.passwordField);
        var $toggleIcon = $(this.config.selectors.toggleIcon);

        if ($toggleButton.length && $passwordField.length && $toggleIcon.length) {
            // Click event
            $toggleButton.on('click', function(e) {
                e.preventDefault();
                self.togglePasswordVisibility($passwordField, $toggleIcon);
            });

            // Keyboard support
            $toggleButton.on('keydown', function(e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    self.togglePasswordVisibility($passwordField, $toggleIcon);
                }
            });

            // Add tooltip
            $toggleButton.attr('title', 'Mostrar/ocultar contraseña');
        }
    },

    /**
     * Initialize form enhancements
     */
    initializeFormEnhancements: function() {
        this.addFloatingLabelAnimations();
        this.addFieldValidationFeedback();
        this.addFormShortcuts();
    },

    /**
     * Enhanced floating label animations
     */
    addFloatingLabelAnimations: function() {
        $('.form-floating input').on('focus blur', function() {
            var $this = $(this);
            var $parent = $this.closest('.form-floating');
            
            if ($this.is(':focus') || $this.val()) {
                $parent.addClass('focused');
            } else {
                $parent.removeClass('focused');
            }
        });
    },

    /**
     * Add real-time field validation feedback
     */
    addFieldValidationFeedback: function() {
        var inputs = $(this.config.selectors.usernameField + ', ' + this.config.selectors.passwordField);
        
        inputs.on('input', function() {
            var $this = $(this);
            var $parent = $this.closest('.form-floating');
            
            if (this.checkValidity()) {
                $parent.removeClass('has-error').addClass('has-success');
            } else if ($this.val()) {
                $parent.removeClass('has-success').addClass('has-error');
            } else {
                $parent.removeClass('has-success has-error');
            }
        });
    },

    /**
     * Add keyboard shortcuts
     */
    addFormShortcuts: function() {
        var self = this;
        
        // Enter key in username field moves to password
        $(this.config.selectors.usernameField).on('keypress', function(e) {
            if (e.which === 13) { // Enter key
                e.preventDefault();
                $(self.config.selectors.passwordField).focus();
            }
        });

        // Ctrl+Enter submits form
        $(document).on('keydown', function(e) {
            if (e.ctrlKey && e.which === 13) {
                $(self.config.selectors.form).submit();
            }
        });
    },

    /**
     * Bind additional events
     */
    bindEvents: function() {
        var self = this;

        // Handle form submission errors
        if ($(this.config.selectors.alertContainer).length > 0) {
            this.hideLoadingState();
            this.shakeForm();
        }

        // Auto-hide alerts after 5 seconds
        setTimeout(function() {
            $(self.config.selectors.alertContainer).fadeOut('slow');
        }, 5000);
    },

    /**
     * Set focus to username field
     */
    setUsernameFocus: function() {
        var $usernameField = $(this.config.selectors.usernameField);
        if ($usernameField.length) {
            // Small delay to ensure page is fully loaded
            setTimeout(function() {
                $usernameField.focus();
            }, 100);
        }
    },

    /**
     * Toggle password field visibility with smooth animations
     */
    togglePasswordVisibility: function($passwordField, $toggleIcon) {
        var isPassword = $passwordField.attr('type') === 'password';
        
        if (isPassword) {
            // Show password
            $passwordField.attr('type', 'text');
            $toggleIcon.removeClass('bi-eye').addClass('bi-eye-slash');
            $passwordField.attr('aria-label', 'Contraseña visible');
        } else {
            // Hide password
            $passwordField.attr('type', 'password');
            $toggleIcon.removeClass('bi-eye-slash').addClass('bi-eye');
            $passwordField.attr('aria-label', 'Contraseña oculta');
        }

        // Brief animation to indicate change
        $toggleIcon.closest('button').addClass('pressed');
        setTimeout(function() {
            $toggleIcon.closest('button').removeClass('pressed');
        }, 150);
    },

    /**
     * Show loading state on form submission
     */
    showLoadingState: function() {
        var $submitButton = $(this.config.selectors.submitButton);
        
        if ($submitButton.length) {
            $submitButton.prop('disabled', true)
                        .data('original-text', $submitButton.html())
                        .html(this.config.messages.loading)
                        .addClass(this.config.classes.loading);

            // Add loading spinner to form
            $(this.config.selectors.form).addClass('loading');
        }
    },

    /**
     * Hide loading state
     */
    hideLoadingState: function() {
        var $submitButton = $(this.config.selectors.submitButton);
        var originalText = $submitButton.data('original-text') || this.config.messages.originalButton;
        
        if ($submitButton.length) {
            $submitButton.prop('disabled', false)
                        .html(originalText)
                        .removeClass(this.config.classes.loading);

            $(this.config.selectors.form).removeClass('loading');
        }
    },

    /**
     * Show validation errors with animation
     */
    showValidationErrors: function($form) {
        $form.find('.form-control:invalid').first().focus();
        this.shakeForm();
    },

    /**
     * Add shake animation to form on error
     */
    shakeForm: function() {
        var $form = $(this.config.selectors.form);
        $form.addClass(this.config.classes.shake);
        
        setTimeout(function() {
            $form.removeClass('shake-error');
        }, 600);
    }
};

// Additional CSS for animations (added dynamically)
$(document).ready(function() {
    $('<style>')
        .prop('type', 'text/css')
        .html(`
            .form-floating.focused label {
                color: #0073d8;
            }
            
            .form-floating.has-success .form-control {
                border-color: #198754;
            }
            
            .form-floating.has-error .form-control {
                border-color: #dc3545;
            }
            
            .btn-loading {
                pointer-events: none;
                opacity: 0.8;
            }
            
            .pressed {
                transform: scale(0.95);
                transition: transform 0.1s ease;
            }
            
            .shake-error {
                animation: shake 0.6s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
            }
            
            @keyframes shake {
                10%, 90% { transform: translate3d(-1px, 0, 0); }
                20%, 80% { transform: translate3d(2px, 0, 0); }
                30%, 50%, 70% { transform: translate3d(-4px, 0, 0); }
                40%, 60% { transform: translate3d(4px, 0, 0); }
            }
            
            .form-floating input:-webkit-autofill {
                -webkit-box-shadow: 0 0 0 30px #fafafa inset;
            }
        `)
        .appendTo('head');
});