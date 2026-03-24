import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '../../../../translate.pipe';

@Component({
  selector: 'app-group-admin-moderation',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TranslatePipe
  ],
  templateUrl: './moderation.html',
  styleUrls: ['./moderation.css']
})
export class ModerationComponent {
  // PLACEHOLDER page: needs backend support for reports/flags/moderation actions
}
