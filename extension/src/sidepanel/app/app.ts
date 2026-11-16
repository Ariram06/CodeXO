import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  title = 'Codexo';
  readonly bridge = inject(ExtensionBridgeService);
}
