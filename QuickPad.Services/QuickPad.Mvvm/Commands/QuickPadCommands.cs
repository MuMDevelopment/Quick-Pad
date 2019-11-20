﻿using System.Linq;
using System.Reflection;
using QuickPad.Mvvm.Commands.Actions;
using QuickPad.Mvvm.Commands.Clipboard;
using QuickPad.Mvvm.Commands.Editing;
using QuickPad.Mvvm.ViewModels;

namespace QuickPad.Mvvm.Commands
{
    public class QuickPadCommands
    {
        public QuickPadCommands()
        {
        }

        public QuickPadCommands(PasteCommand pasteCommand)
        {
            PasteCommand = pasteCommand;
        }

        public SimpleCommand<DocumentViewModel> SaveCommand { get; } = new SimpleCommand<DocumentViewModel>();
        public SimpleCommand<DocumentViewModel> SaveAsCommand { get; } = new SimpleCommand<DocumentViewModel>();
        public SimpleCommand<DocumentViewModel> LoadCommand { get; } = new SimpleCommand<DocumentViewModel>();
        public SimpleCommand<DocumentViewModel> NewDocumentCommand { get; } = new SimpleCommand<DocumentViewModel>();
        public SimpleCommand<DocumentViewModel> ShareCommand { get; } = new SimpleCommand<DocumentViewModel>();
        public SimpleCommand<DocumentViewModel> ExitCommand { get; } = new SimpleCommand<DocumentViewModel>();

        public SimpleCommand<DocumentViewModel> UndoCommand { get; } = new UndoCommand();
        public SimpleCommand<DocumentViewModel> RedoCommand { get; } = new RedoCommand();

        //clipboard
        public SimpleCommand<DocumentViewModel> CutCommand { get; } = new CutCommand();
        public SimpleCommand<DocumentViewModel> CopyCommand { get; } = new CopyCommand();
        public SimpleCommand<DocumentViewModel> PasteCommand { get; }

        public SimpleCommand<DocumentViewModel> EmojiCommand { get; } = new EmojiCommand();

        public SimpleCommand<DocumentViewModel> BoldCommand { get; } = new BoldCommand();
        public SimpleCommand<DocumentViewModel> ItalicsCommand { get; } = new ItalicCommand();
        public SimpleCommand<DocumentViewModel> UnderlineCommand { get; } = new UnderlineCommand();
        public SimpleCommand<DocumentViewModel> StrikeThroughCommand { get; } = new StrikeThroughCommand();
        public SimpleCommand<DocumentViewModel> BulletsCommand { get; } = new BulletsCommand();
        public SimpleCommand<DocumentViewModel> LeftAlignCommand { get; } = new LeftAlignCommand();
        public SimpleCommand<DocumentViewModel> CenterAlignCommand { get; } = new CenterAlignCommand();
        public SimpleCommand<DocumentViewModel> RightAlignCommand { get; } = new RightAlignCommand();
        public SimpleCommand<DocumentViewModel> JustifyCommand { get; } = new JustifyCommand();

      //actions
        public SimpleCommand<SettingsViewModel> SettingsCommand { get; } = new ShowSettingsCommand();
        public SimpleCommand<SettingsViewModel> ShowCommandBarCommand { get; } = new ShowCommandBarCommand();
        public SimpleCommand<SettingsViewModel> ShowMenusCommand { get; } = new ShowMenusCommand();
        public SimpleCommand<SettingsViewModel> ResetSettingsCommand { get; } = new ResetSettingsCommand();
        public SimpleCommand<SettingsViewModel> ImportSettingsCommand { get; } = new ImportSettingsCommand();
        public SimpleCommand<SettingsViewModel> ExportSettingsCommand { get; } = new ExportSettingsCommand();
        public SimpleCommand<SettingsViewModel> AcknowledgeFontSelectionChangeCommand { get; } = new AcknowledgeFontSelectionChangeCommand();

        public void NotifyChanged(DocumentViewModel documentViewModel, SettingsViewModel settingsViewModel)
        {
            GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(pi => pi.PropertyType == typeof(SimpleCommand<DocumentViewModel>) || pi.PropertyType == typeof(SimpleCommand<SettingsViewModel>))
                .ToList().ForEach(pi =>
                {
                    var documentCommand = pi.GetValue(this) as SimpleCommand<DocumentViewModel>;
                    documentCommand?.InvokeCanExecuteChanged(documentViewModel);

                    var settingsCommand = pi.GetValue(this) as SimpleCommand<SettingsViewModel>;
                    settingsCommand?.InvokeCanExecuteChanged(settingsViewModel);
                });
        }
    }
}