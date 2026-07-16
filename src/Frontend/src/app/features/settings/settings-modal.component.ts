import { Component, EventEmitter, Input, Output, signal, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-settings-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings-modal.component.html',
  styleUrl: './settings-modal.component.css'
})
export class SettingsModalComponent implements OnChanges {
  @Input({ required: true }) isOpen: boolean = false;
  @Input({ required: true }) enabled: boolean = false;
  @Input({ required: true }) provider: string = 'Gemini';
  @Input({ required: true }) apiKey: string = '';
  @Input({ required: true }) modelName: string = 'gemini-1.5-flash';

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<{
    enabled: boolean;
    provider: string;
    apiKey: string;
    modelName: string;
  }>();

  readonly aiEnabled = signal<boolean>(false);
  readonly aiProvider = signal<string>('Gemini');
  readonly aiApiKey = signal<string>('');
  readonly aiModelName = signal<string>('gemini-1.5-flash');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.aiEnabled.set(this.enabled);
      this.aiProvider.set(this.provider);
      this.aiApiKey.set(this.apiKey);
      this.aiModelName.set(this.modelName);
    }
  }

  onCloseClick(): void {
    this.close.emit();
  }

  onSaveClick(): void {
    this.save.emit({
      enabled: this.aiEnabled(),
      provider: this.aiProvider(),
      apiKey: this.aiApiKey(),
      modelName: this.aiModelName()
    });
  }
}
