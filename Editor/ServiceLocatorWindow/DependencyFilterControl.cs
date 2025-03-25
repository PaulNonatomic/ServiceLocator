using System;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
    /// <summary>
    /// Control panel for filtering service dependencies and dependents
    /// </summary>
    public class DependencyFilterControl : VisualElement
    {
        // Dependency visualization filter modes
        public enum FilterMode
        {
            All,          // Show all services
            Dependencies, // Show only dependencies of selected service
            Dependents,   // Show only dependents of selected service
            Both          // Show both dependencies and dependents
        }
        
        // Current filter state
        public FilterMode CurrentMode { get; private set; } = FilterMode.Both;
        public bool IsFilterActive { get; private set; } = false;
        public Type SelectedServiceType { get; private set; } = null;
        
        // UI Elements
        private readonly Button _allButton;
        private readonly Button _dependenciesButton;
        private readonly Button _dependentsButton;
        private readonly Button _bothButton;
        private readonly Button _clearButton;
        private readonly Label _statusLabel;
        
        // Events
        public event Action<Type, FilterMode> OnFilterChanged;
        public event Action OnFilterCleared;
        
        public DependencyFilterControl()
        {
            AddToClassList("dependency-filter-control");
            
            // Create UI elements
            var filterButtonsRow = new VisualElement();
            filterButtonsRow.AddToClassList("filter-buttons-row");
            Add(filterButtonsRow);
            
            // Filter buttons with fixed action handlers to ensure clicks are always captured
            _allButton = new Button() { text = "All Services" };
            _allButton.clicked += () => SetFilterMode(FilterMode.All);
            _allButton.AddToClassList("filter-button");
            filterButtonsRow.Add(_allButton);
            
            _dependenciesButton = new Button() { text = "Dependencies" };
            _dependenciesButton.clicked += () => SetFilterMode(FilterMode.Dependencies);
            _dependenciesButton.AddToClassList("filter-button");
            filterButtonsRow.Add(_dependenciesButton);
            
            _dependentsButton = new Button() { text = "Dependents" };
            _dependentsButton.clicked += () => SetFilterMode(FilterMode.Dependents);
            _dependentsButton.AddToClassList("filter-button");
            filterButtonsRow.Add(_dependentsButton);
            
            _bothButton = new Button() { text = "Both" };
            _bothButton.clicked += () => SetFilterMode(FilterMode.Both);
            _bothButton.AddToClassList("filter-button");
            filterButtonsRow.Add(_bothButton);
            
            _clearButton = new Button() { text = "Clear Filter" };
            _clearButton.clicked += ClearFilter;
            _clearButton.AddToClassList("filter-button");
            _clearButton.AddToClassList("clear-filter-button");
            filterButtonsRow.Add(_clearButton);
            
            // Status label to show current filter
            _statusLabel = new Label("Showing all services");
            _statusLabel.AddToClassList("filter-status-label");
            Add(_statusLabel);
            
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Apply a filter for a specific service type
        /// </summary>
        public void ApplyFilter(Type serviceType, FilterMode mode = FilterMode.Both)
        {
            Debug.Log($"ApplyFilter called with type: {serviceType?.Name ?? "null"}, mode: {mode}");
            
            if (serviceType == null)
            {
                ClearFilter();
                return;
            }

            // If applying the same filter type, just clear it (toggle behavior)
            if (serviceType == SelectedServiceType && mode == CurrentMode)
            {
                ClearFilter();
                return;
            }

            SelectedServiceType = serviceType;
            CurrentMode = mode;
            IsFilterActive = true;
            
            OnFilterChanged?.Invoke(serviceType, mode);
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Sets the filter mode and applies it if a service is selected
        /// </summary>
        private void SetFilterMode(FilterMode mode)
        {
            Debug.Log($"SetFilterMode called with mode: {mode}, current mode: {CurrentMode}");
            
            // If we're already in this mode and not filtering anything, do nothing
            if (mode == CurrentMode && SelectedServiceType == null && mode != FilterMode.All)
            {
                Debug.Log("Same mode, no service selected - doing nothing");
                return;
            }
            
            // If we're already in this mode and have a selection, update with current selection
            if (mode == CurrentMode && SelectedServiceType != null)
            {
                Debug.Log($"Same mode with selected service {SelectedServiceType.Name} - refreshing");
                OnFilterChanged?.Invoke(SelectedServiceType, mode);
                return;
            }
            
            CurrentMode = mode;
            
            // If a service is selected, apply the new filter mode
            if (SelectedServiceType != null)
            {
                Debug.Log($"Applying filter mode {mode} to selected service {SelectedServiceType.Name}");
                IsFilterActive = true;
                OnFilterChanged?.Invoke(SelectedServiceType, mode);
            }
            else if (mode == FilterMode.All)
            {
                // If "All" is selected and no service is selected, clear the filter
                Debug.Log("All mode with no service selected - clearing filter");
                ClearFilter();
                return;
            }
            else
            {
                // Mode selected but no service - just update button states
                Debug.Log($"Mode {mode} selected but no service - just updating button states");
            }
            
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Clears any active filter
        /// </summary>
        private void ClearFilter()
        {
            Debug.Log("ClearFilter called");
            
            if (!IsFilterActive && SelectedServiceType == null)
            {
                Debug.Log("Filter already cleared - doing nothing");
                return;
            }
                
            IsFilterActive = false;
            SelectedServiceType = null;
            CurrentMode = FilterMode.All;
            
            Debug.Log("Invoking OnFilterCleared");
            OnFilterCleared?.Invoke();
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Updates the active state of filter buttons
        /// </summary>
        private void UpdateButtonStates()
        {
            // Remove active class from all buttons
            _allButton.RemoveFromClassList("filter-active");
            _dependenciesButton.RemoveFromClassList("filter-active");
            _dependentsButton.RemoveFromClassList("filter-active");
            _bothButton.RemoveFromClassList("filter-active");
            
            // Add active class to the appropriate button
            if (!IsFilterActive)
            {
                _allButton.AddToClassList("filter-active");
                _statusLabel.text = "Showing all services";
                return;
            }
            
            string serviceName = SelectedServiceType?.Name ?? "Unknown";
            
            switch (CurrentMode)
            {
                case FilterMode.Dependencies:
                    _dependenciesButton.AddToClassList("filter-active");
                    _statusLabel.text = $"Showing dependencies of {serviceName}";
                    break;
                case FilterMode.Dependents:
                    _dependentsButton.AddToClassList("filter-active");
                    _statusLabel.text = $"Showing dependents of {serviceName}";
                    break;
                case FilterMode.Both:
                    _bothButton.AddToClassList("filter-active");
                    _statusLabel.text = $"Showing dependencies and dependents of {serviceName}";
                    break;
                case FilterMode.All:
                    _allButton.AddToClassList("filter-active");
                    _statusLabel.text = $"Filtered view for {serviceName}";
                    break;
            }
            
            // Add styling to status label based on filter type
            _statusLabel.RemoveFromClassList("dependency-status");
            _statusLabel.RemoveFromClassList("dependent-status");
            _statusLabel.RemoveFromClassList("both-status");
            
            switch (CurrentMode)
            {
                case FilterMode.Dependencies:
                    _statusLabel.AddToClassList("dependency-status");
                    break;
                case FilterMode.Dependents:
                    _statusLabel.AddToClassList("dependent-status");
                    break;
                case FilterMode.Both:
                    _statusLabel.AddToClassList("both-status");
                    break;
            }
            
            // Update clear button visibility based on filter state
            if (IsFilterActive)
            {
                _clearButton.RemoveFromClassList("hidden");
            }
            else
            {
                _clearButton.AddToClassList("hidden");
            }
        }
    }
}