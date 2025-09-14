// File: ReolMarket.Core/BaseViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ReolMarket.Data;
using ReolMarket.Core;

namespace ReolMarket.Core
{
    public abstract class BaseViewModel<TRepo, TModel, TKey> : INotifyPropertyChanged
        where TRepo  : IBaseRepository<TModel, TKey>
        where TModel : class, new()
        where TKey   : notnull
    {
        protected readonly TRepo Repository;

        protected BaseViewModel(TRepo repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Items = Repository.Items;

            AddCommand     = new RelayCommand(_ => OnAdd(),    _ => Selected != null);
            UpdateCommand  = new RelayCommand(_ => OnUpdate(), _ => Selected != null);
            DeleteCommand  = new RelayCommand(_ => OnDelete(), _ => Selected != null);
            RefreshCommand = new RelayCommand(_ => OnRefresh());
        }

        // ----------------- State -----------------
        public ObservableCollection<TModel> Items { get; }

        private TModel? _selected;
        public TModel? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                    RaiseCommandCanExecuteChanged();
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ----------------- Commands -----------------
        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        // ----------------- CRUD -----------------
        protected virtual void OnAdd()
        {
            if (Selected is null) return;
            Repository.Add(Selected);
            StatusMessage = "Tilføjet.";
        }

        protected virtual void OnUpdate()
        {
            if (Selected is null) return;
            Repository.Update(Selected);
            StatusMessage = "Opdateret.";
        }

        protected virtual void OnDelete()
        {
            if (Selected is null) return;

            var id = GetKey(Selected);
            Repository.Delete(id);

            var toRemove = Items.FirstOrDefault(x => Equals(GetKey(x), id)) ?? Selected;
            if (toRemove is not null) Items.Remove(toRemove);

            Selected = Items.FirstOrDefault();
            StatusMessage = "Slettet.";
        }

        protected virtual void OnRefresh()
        {
            Items.Clear();
            foreach (var e in Repository.GetAll())
                Items.Add(e);

            if (!Items.Contains(Selected)) Selected = Items.FirstOrDefault();
            StatusMessage = "Opdateret.";
        }

        // ----------------- Key resolver -----------------
        protected abstract TKey GetKey(TModel entity);

        // ----------------- INotifyPropertyChanged -----------------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<TProp>(ref TProp storage, TProp value, [CallerMemberName] string? name = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(name);
            return true;
        }

        private void RaiseCommandCanExecuteChanged()
        {
            (AddCommand    as RelayCommand)?.RaiseCanExecuteChanged();
            (UpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}