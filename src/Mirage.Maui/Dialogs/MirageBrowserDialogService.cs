using System.Reflection;
using Mirage.Core.Diagnostics;
using Mirage.Maui.Handlers;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace Mirage.Maui.Dialogs;

internal sealed class MirageBrowserDialogService : IMirageBrowserDialogService, IDisposable
{
    private const string DialogHostAutomationId = "mirage-dialog-host";
    private const string DialogOverlayAutomationId = "mirage-dialog-overlay";
    private const string DialogBackdropAutomationId = "mirage-dialog-backdrop";
    private const string DialogSurfaceAutomationId = "mirage-dialog-surface";
    private const string DialogTitleAutomationId = "mirage-dialog-title";
    private const string DialogMessageAutomationId = "mirage-dialog-message";
    private const string DialogPromptInputAutomationId = "mirage-dialog-input";
    private const string DialogAcceptAutomationId = "mirage-dialog-accept";
    private const string DialogCancelAutomationId = "mirage-dialog-cancel";
    private const string DialogDestructionAutomationId = "mirage-dialog-destruction";
    private const string DialogFooterAutomationId = "mirage-dialog-footer";
    private static readonly TimeSpan FocusDispatchDelay = TimeSpan.FromMilliseconds(16);
    private static readonly Type? AlertManagerType = typeof(Page).Assembly.GetType("Microsoft.Maui.Controls.Platform.AlertManager");
    private static readonly Type? AlertManagerSubscriptionType = typeof(Page).Assembly.GetType("Microsoft.Maui.Controls.Platform.AlertManager+IAlertManagerSubscription");
    private static readonly PropertyInfo? WindowAlertManagerProperty = typeof(Window).GetProperty("AlertManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly PropertyInfo? AlertManagerSubscriptionProperty = AlertManagerType?.GetProperty("Subscription", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? AlertManagerSubscriptionField = AlertManagerType?.GetField("_subscription", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo? AlertManagerSubscribeMethod = AlertManagerType?.GetMethod("Subscribe", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? AlertManagerUnsubscribeMethod = AlertManagerType?.GetMethod("Unsubscribe", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? DispatchProxyCreateMethod = typeof(DispatchProxy)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .SingleOrDefault(method =>
            string.Equals(method.Name, nameof(DispatchProxy.Create), StringComparison.Ordinal)
            && method.IsGenericMethodDefinition
            && method.GetGenericArguments().Length == 2);

    private readonly Dictionary<Window, WindowDialogState> _states = new(ReferenceEqualityComparer.Instance);
    private bool _disposed;

    public void AttachWindow(Window window, MiragePlatformView host, IMirageRenderCoordinator coordinator)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(coordinator);

        ThrowIfDisposed();

        if (_states.TryGetValue(window, out var existing))
        {
            existing.Rebind(host, coordinator);
            existing.EnsureAlertSubscription();
            return;
        }

        var state = new WindowDialogState(window, host, coordinator);
        state.ConfigureHost();
        state.EnsureAlertSubscription();
        _states[window] = state;
    }

    public void CancelDialogs(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_states.TryGetValue(window, out var state))
        {
            state.CancelAll();
        }
    }

    public void DetachWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_states.Remove(window, out var state))
        {
            state.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var state in _states.Values.ToArray())
        {
            state.Dispose();
        }

        _states.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static object CreateAlertManagerSubscription(WindowDialogState state)
    {
        if (AlertManagerSubscriptionType is null || DispatchProxyCreateMethod is null)
        {
            throw CreateAlertManagerBindingException("subscription interface");
        }

        var createMethod = DispatchProxyCreateMethod.MakeGenericMethod(AlertManagerSubscriptionType, typeof(AlertManagerDispatchProxy));
        var proxy = createMethod.Invoke(null, null)
            ?? throw CreateAlertManagerBindingException("subscription proxy");

        if (proxy is not AlertManagerDispatchProxy dispatchProxy)
        {
            throw CreateAlertManagerBindingException("subscription proxy cast");
        }

        dispatchProxy.State = state;
        return proxy;
    }

    private static Exception CreateAlertManagerBindingException(string missingPart)
    {
        var message = $"Mirage dialog foundation could not bind MAUI AlertManager {missingPart}. Upstream MAUI likely changed and the dialog bridge must be updated.";
        MirageDiagnostics.Log("dialog.service", message);
        return new InvalidOperationException(message);
    }

    private class AlertManagerDispatchProxy : DispatchProxy
    {
        public WindowDialogState? State { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null || State is null)
            {
                return null;
            }

            switch (targetMethod.Name)
            {
                case "OnAlertRequested" when args is [Page page, AlertArguments arguments]:
                    State.OnAlertRequested(page, arguments);
                    break;
                case "OnPromptRequested" when args is [Page page, PromptArguments arguments]:
                    State.OnPromptRequested(page, arguments);
                    break;
                case "OnActionSheetRequested" when args is [Page page, ActionSheetArguments arguments]:
                    State.OnActionSheetRequested(page, arguments);
                    break;
                case "OnPageBusy":
                    break;
            }

            return null;
        }
    }

    private sealed class WindowDialogState
    {
        private readonly Queue<DialogRequest> _pending = new();
        private readonly List<MiragePlatformView> _activeNodes = [];
        private readonly List<MiragePlatformView> _focusableNodes = [];

        private DialogRequest? _activeRequest;
        private object? _alertManager;
        private object? _alertManagerSubscription;
        private bool _isAlertManagerSubscribed;
        private string? _restoreFocusNodeId;
        private string? _promptInputNodeId;
        private MiragePlatformView _host;
        private IMirageRenderCoordinator _coordinator;

        public WindowDialogState(Window window, MiragePlatformView host, IMirageRenderCoordinator coordinator)
        {
            Window = window;
            _host = host;
            _coordinator = coordinator;
        }

        public Window Window { get; }

        public void Rebind(MiragePlatformView host, IMirageRenderCoordinator coordinator)
        {
            _host = host;
            _coordinator = coordinator;
            ConfigureHost();
            _coordinator.RequestRender();
        }

        public void ConfigureHost()
        {
            _host.SetProperty("AutomationId", DialogHostAutomationId);
            _host.SetAttribute("aria-hidden", _activeRequest is null ? "true" : "false");
            _host.AddClass("mirage-dialog-host");
            _host.SetStyle("position", "absolute");
            _host.SetStyle("inset", "0");
            _host.SetStyle("pointerEvents", "none");
            _host.SetStyle("zIndex", "2147483000");
        }

        public void Enqueue(DialogRequest request)
        {
            _pending.Enqueue(request);
            TryShowNext();
        }

        public void CancelAll()
        {
            _activeRequest?.Cancel();
            _activeRequest = null;

            while (_pending.Count > 0)
            {
                _pending.Dequeue().Cancel();
            }

            ResetVisualState();
        }

        public void Dispose()
        {
            CancelAll();
            ReleaseAlertSubscription();
            _host.ClearChildren();
        }

        public void EnsureAlertSubscription()
        {
            if (_isAlertManagerSubscribed)
            {
                return;
            }

            _alertManager ??= WindowAlertManagerProperty?.GetValue(Window)
                ?? throw CreateAlertManagerBindingException("window AlertManager");
            _alertManagerSubscription ??= CreateAlertManagerSubscription(this);

            if (AlertManagerSubscriptionProperty?.SetMethod is { } setMethod)
            {
                setMethod.Invoke(_alertManager, [_alertManagerSubscription]);
            }
            else if (AlertManagerSubscriptionField is not null)
            {
                AlertManagerSubscriptionField.SetValue(_alertManager, _alertManagerSubscription);
            }
            else
            {
                throw CreateAlertManagerBindingException("subscription setter");
            }

            if (AlertManagerSubscribeMethod is null)
            {
                throw CreateAlertManagerBindingException("Subscribe()");
            }

            AlertManagerSubscribeMethod.Invoke(_alertManager, null);
            _isAlertManagerSubscribed = true;
        }

        public void OnAlertRequested(Page page, AlertArguments arguments)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(arguments);
            Enqueue(new AlertDialogRequest(page, arguments));
        }

        public void OnPromptRequested(Page page, PromptArguments arguments)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(arguments);
            Enqueue(new PromptDialogRequest(page, arguments));
        }

        public void OnActionSheetRequested(Page page, ActionSheetArguments arguments)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(arguments);
            Enqueue(new ActionSheetDialogRequest(page, arguments));
        }

        private void TryShowNext()
        {
            if (_activeRequest is not null || _pending.Count == 0)
            {
                return;
            }

            _activeRequest = _pending.Dequeue();
            BuildDialog(_activeRequest);
        }

        private void BuildDialog(DialogRequest request)
        {
            if (string.IsNullOrWhiteSpace(_restoreFocusNodeId))
            {
                _coordinator.TryGetActiveNodeId(out _restoreFocusNodeId);
            }

            _promptInputNodeId = null;
            ClearActiveNodes();
            ConfigureHost();
            _host.SetAttribute("aria-hidden", "false");

            using var _ = _coordinator.BeginRenderBatch();

            var overlay = CreateNode("div", DialogOverlayAutomationId, null, "mirage-dialog-overlay");
            overlay.SetAttribute("data-mirage-dialog-presentation", request.PresentationAttribute);
            overlay.SetAttribute("data-mirage-dialog-kind", request.Kind);

            var backdrop = CreateEventNode(
                "div",
                DialogBackdropAutomationId,
                () =>
                {
                    if (request.CanDismissWithBackdrop)
                    {
                        request.Cancel();
                        CompleteActiveDialog();
                    }
                },
                onKeyDown: null,
                text: null,
                false,
                "mirage-dialog-backdrop");
            backdrop.SetAttribute("aria-hidden", "true");

            var surface = CreateNode("div", DialogSurfaceAutomationId, null, "mirage-dialog-surface");
            surface.SetAttribute("data-mirage-dialog-presentation", request.PresentationAttribute);
            surface.SetAttribute("data-mirage-dialog-kind", request.Kind);
            surface.SetAttribute("role", request.AriaRole);
            surface.SetAttribute("aria-modal", "true");
            surface.SetAttribute("aria-labelledby", DialogTitleAutomationId);
            if (!string.IsNullOrWhiteSpace(request.Message))
            {
                surface.SetAttribute("aria-describedby", DialogMessageAutomationId);
            }

            var title = CreateNode("div", DialogTitleAutomationId, request.Title, "mirage-dialog-title");
            title.SetAttribute("id", DialogTitleAutomationId);
            surface.AddChild(title);

            if (!string.IsNullOrWhiteSpace(request.Message))
            {
                var message = CreateNode("div", DialogMessageAutomationId, request.Message, "mirage-dialog-message");
                message.SetAttribute("id", DialogMessageAutomationId);
                surface.AddChild(message);
            }

            request.BuildContent(this, surface);

            var footer = CreateNode("div", DialogFooterAutomationId, null, "mirage-dialog-footer");
            footer.SetAttribute("data-mirage-dialog-presentation", request.PresentationAttribute);
            request.BuildButtons(this, footer);
            surface.AddChild(footer);

            overlay.AddChild(backdrop);
            overlay.AddChild(surface);
            _host.AddChild(overlay);
            _coordinator.RequestRender();
            ScheduleInitialFocus();
        }

        private void ScheduleInitialFocus()
        {
            var targetNodeId = _promptInputNodeId ?? _focusableNodes.FirstOrDefault()?.NodeId;
            if (string.IsNullOrWhiteSpace(targetNodeId))
            {
                return;
            }

            _coordinator.DispatchDelayed(FocusDispatchDelay, () => _coordinator.TrySetNodeFocus(targetNodeId, isFocused: true));
        }

        private void CompleteActiveDialog()
        {
            ResetVisualState();
            TryShowNext();
        }

        private void ResetVisualState()
        {
            var shouldRestoreFocus = _pending.Count == 0;
            ClearActiveNodes();
            _host.ClearChildren();
            _host.SetAttribute("aria-hidden", "true");
            _coordinator.RequestRender();

            if (shouldRestoreFocus && !string.IsNullOrWhiteSpace(_restoreFocusNodeId))
            {
                var focusNodeId = _restoreFocusNodeId;
                _coordinator.DispatchDelayed(
                    FocusDispatchDelay,
                    () => _coordinator.TrySetNodeFocus(focusNodeId, isFocused: true, suppressNextFocusEvent: true));
                _restoreFocusNodeId = null;
            }

            _promptInputNodeId = null;
            _activeRequest = null;
        }

        private void ReleaseAlertSubscription()
        {
            if (_alertManager is null)
            {
                return;
            }

            if (_isAlertManagerSubscribed && AlertManagerUnsubscribeMethod is not null)
            {
                AlertManagerUnsubscribeMethod.Invoke(_alertManager, null);
            }

            _isAlertManagerSubscribed = false;

            if (AlertManagerSubscriptionProperty?.SetMethod is { } setMethod)
            {
                setMethod.Invoke(_alertManager, [null]);
            }
            else if (AlertManagerSubscriptionField is not null)
            {
                AlertManagerSubscriptionField.SetValue(_alertManager, null);
            }

            _alertManagerSubscription = null;
            _alertManager = null;
        }

        private void ClearActiveNodes()
        {
            _focusableNodes.Clear();

            foreach (var node in _activeNodes)
            {
                _coordinator.Unregister(node);
            }

            _activeNodes.Clear();
        }

        private MiragePlatformView CreateNode(string elementName, string? automationId, string? text, params string[] classes)
        {
            var node = MiragePlatformView.CreateWeb(elementName);
            RegisterNode(node, eventTarget: null, automationId, text, classes);
            return node;
        }

        private MiragePlatformView CreateEventNode(
            string elementName,
            string? automationId,
            Action onClick,
            Action<string?>? onKeyDown,
            string? text,
            bool includeInFocusOrder = true,
            params string[] classes)
        {
            var node = MiragePlatformView.CreateWeb(elementName);
            node.SetEvent("click", enabled: true);
            if (onKeyDown is not null)
            {
                node.SetEvent("keydown", enabled: true);
            }

            var target = new NodeEventTarget(onClick, onKeyDown);
            RegisterNode(node, target, automationId, text, classes);
            if (includeInFocusOrder)
            {
                _focusableNodes.Add(node);
            }

            return node;
        }

        private void RegisterNode(
            MiragePlatformView node,
            IMirageRuntimeEventTarget? eventTarget,
            string? automationId,
            string? text,
            params string[] classes)
        {
            _coordinator.Register(node, eventTarget);
            _activeNodes.Add(node);

            if (!string.IsNullOrWhiteSpace(automationId))
            {
                node.SetProperty("AutomationId", automationId);
            }

            if (text is not null)
            {
                node.SetProperty("textContent", text);
            }

            foreach (var className in classes)
            {
                if (!string.IsNullOrWhiteSpace(className))
                {
                    node.AddClass(className);
                }
            }
        }

        public void AddFooterButton(
            MiragePlatformView footer,
            string text,
            string automationId,
            string role,
            Action onClick,
            bool includeKeyHandling = true)
        {
            var button = CreateEventNode(
                "button",
                automationId,
                () =>
                {
                    onClick();
                    CompleteActiveDialog();
                },
                includeKeyHandling ? HandleKeyDismiss : null,
                text,
                true,
                "mirage-dialog-button");
            button.SetAttribute("type", "button");
            button.SetAttribute("data-mirage-dialog-button-role", role);
            footer.AddChild(button);
        }

        public MiragePlatformView AddPromptInput(MiragePlatformView surface, PromptDialogRequest request)
        {
            var input = CreateEventNode(
                "input",
                DialogPromptInputAutomationId,
                onClick: () => { },
                onKeyDown: key => HandlePromptKeyDown(request, key),
                text: null,
                classes: "mirage-dialog-input");
            input.SetAttribute("type", "text");
            input.SetAttribute("placeholder", request.Placeholder);
            input.SetAttribute("maxlength", request.MaxLength > 0 ? request.MaxLength.ToString() : null);
            input.SetAttribute("inputmode", request.InputMode);
            input.SetProperty("value", request.InitialValue ?? string.Empty);
            surface.AddChild(input);
            _promptInputNodeId = input.NodeId;
            return input;
        }

        private void HandlePromptKeyDown(PromptDialogRequest request, string? payload)
        {
            if (string.Equals(payload, "Enter", StringComparison.Ordinal))
            {
                request.Accept(ReadPromptValue());
                CompleteActiveDialog();
                return;
            }

            if (string.Equals(payload, "Escape", StringComparison.Ordinal) && request.CanDismissWithBackdrop)
            {
                request.Cancel();
                CompleteActiveDialog();
            }
        }

        private void HandleKeyDismiss(string? payload)
        {
            if (!string.Equals(payload, "Escape", StringComparison.Ordinal))
            {
                return;
            }

            if (_activeRequest?.CanDismissWithBackdrop != true)
            {
                return;
            }

            _activeRequest.Cancel();
            CompleteActiveDialog();
        }

        internal string ReadPromptValue()
        {
            if (string.IsNullOrWhiteSpace(_promptInputNodeId))
            {
                return string.Empty;
            }

            return _coordinator.TryGetNodeInputValue(_promptInputNodeId, out var value)
                ? value ?? string.Empty
                : string.Empty;
        }

        private sealed class NodeEventTarget(Action onClick, Action<string?>? onKeyDown) : IMirageRuntimeEventTarget
        {
            public void OnRuntimeEvent(string eventName, string? payload)
            {
                if (string.Equals(eventName, "click", StringComparison.Ordinal))
                {
                    onClick();
                    return;
                }

                if (string.Equals(eventName, "keydown", StringComparison.Ordinal))
                {
                    onKeyDown?.Invoke(payload);
                }
            }
        }
    }

    private abstract class DialogRequest(Page page)
    {
        public Page Page { get; } = page;

        public abstract string Kind { get; }

        public abstract string Title { get; }

        public abstract string? Message { get; }

        public abstract string PresentationAttribute { get; }

        public abstract string AriaRole { get; }

        public abstract bool CanDismissWithBackdrop { get; }

        public virtual void BuildContent(WindowDialogState state, MiragePlatformView surface)
        {
            _ = state;
            _ = surface;
        }

        public abstract void BuildButtons(WindowDialogState state, MiragePlatformView footer);

        public abstract void Cancel();
    }

    private sealed class AlertDialogRequest(Page page, AlertArguments arguments) : DialogRequest(page)
    {
        public override string Kind => "alert";

        public override string Title => arguments.Title ?? string.Empty;

        public override string? Message => arguments.Message;

        public override string PresentationAttribute => "modal";

        public override string AriaRole => "alertdialog";

        public override bool CanDismissWithBackdrop => !string.IsNullOrWhiteSpace(arguments.Cancel);

        public override void BuildButtons(WindowDialogState state, MiragePlatformView footer)
        {
            var acceptText = arguments.Accept;
            var cancelText = arguments.Cancel;

            if (!string.IsNullOrWhiteSpace(acceptText) && !string.IsNullOrWhiteSpace(cancelText))
            {
                state.AddFooterButton(footer, cancelText, DialogCancelAutomationId, "cancel", () => arguments.SetResult(false));
                state.AddFooterButton(footer, acceptText, DialogAcceptAutomationId, "primary", () => arguments.SetResult(true));
                return;
            }

            var primaryText = !string.IsNullOrWhiteSpace(acceptText)
                ? acceptText
                : !string.IsNullOrWhiteSpace(cancelText)
                    ? cancelText
                    : "OK";

            state.AddFooterButton(footer, primaryText, DialogAcceptAutomationId, "primary", () => arguments.SetResult(true));
        }

        public override void Cancel() => arguments.SetResult(false);
    }

    private sealed class ActionSheetDialogRequest(Page page, ActionSheetArguments arguments) : DialogRequest(page)
    {
        public override string Kind => "action-sheet";

        public override string Title => arguments.Title ?? string.Empty;

        public override string? Message => null;

        public override string PresentationAttribute => "sheet";

        public override string AriaRole => "dialog";

        public override bool CanDismissWithBackdrop => !string.IsNullOrWhiteSpace(arguments.Cancel);

        public override void BuildButtons(WindowDialogState state, MiragePlatformView footer)
        {
            var optionIndex = 0;
            foreach (var button in arguments.Buttons ?? Enumerable.Empty<string>())
            {
                var selected = button;
                state.AddFooterButton(
                    footer,
                    selected,
                    $"mirage-dialog-option-{optionIndex}",
                    "default",
                    () => arguments.SetResult(selected));
                optionIndex++;
            }

            if (!string.IsNullOrWhiteSpace(arguments.Destruction))
            {
                var destruction = arguments.Destruction;
                state.AddFooterButton(footer, destruction, DialogDestructionAutomationId, "destructive", () => arguments.SetResult(destruction));
            }

            if (!string.IsNullOrWhiteSpace(arguments.Cancel))
            {
                var cancel = arguments.Cancel;
                state.AddFooterButton(footer, cancel, DialogCancelAutomationId, "cancel", () => arguments.SetResult(cancel));
            }
        }

        public override void Cancel() => arguments.SetResult(arguments.Cancel);
    }

    private sealed class PromptDialogRequest(Page page, PromptArguments arguments) : DialogRequest(page)
    {
        public override string Kind => "prompt";

        public override string Title => arguments.Title ?? string.Empty;

        public override string? Message => arguments.Message;

        public override string PresentationAttribute => "modal";

        public override string AriaRole => "dialog";

        public override bool CanDismissWithBackdrop => false;

        public string? Placeholder => arguments.Placeholder;

        public string? InitialValue => arguments.InitialValue;

        public int MaxLength => arguments.MaxLength;

        public string? InputMode => MapKeyboardInputMode(arguments.Keyboard);

        public override void BuildContent(WindowDialogState state, MiragePlatformView surface)
        {
            state.AddPromptInput(surface, this);
        }

        public override void BuildButtons(WindowDialogState state, MiragePlatformView footer)
        {
            if (!string.IsNullOrWhiteSpace(arguments.Cancel))
            {
                state.AddFooterButton(footer, arguments.Cancel, DialogCancelAutomationId, "cancel", Cancel);
            }

            state.AddFooterButton(footer, arguments.Accept, DialogAcceptAutomationId, "primary", () => Accept(state.ReadPromptValue()));
        }

        public void Accept(string? value)
        {
            arguments.SetResult(value);
        }

        public override void Cancel() => arguments.SetResult(null!);

        private static string? MapKeyboardInputMode(Keyboard? keyboard)
        {
            if (keyboard is null)
            {
                return null;
            }

            if (ReferenceEquals(keyboard, Keyboard.Numeric))
            {
                return "numeric";
            }

            if (ReferenceEquals(keyboard, Keyboard.Telephone))
            {
                return "tel";
            }

            if (ReferenceEquals(keyboard, Keyboard.Email))
            {
                return "email";
            }

            if (ReferenceEquals(keyboard, Keyboard.Url))
            {
                return "url";
            }

            return null;
        }
    }
}
