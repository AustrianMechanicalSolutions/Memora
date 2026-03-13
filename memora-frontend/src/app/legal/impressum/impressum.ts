import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-impressum',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './impressum.html',
  styleUrls: ['./impressum.css']
})
export class ImpressumComponent {
  readonly teamMembers = [
    'Nico Kogler',
    'Mikhail Krech',
    'Nosakhare Jegumna',
    'Petar Gajic',
    'Filip Grgic'
  ];
}
