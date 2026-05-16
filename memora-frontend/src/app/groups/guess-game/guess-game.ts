import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MemoryDto, AlbumDto } from '../groups';
import { environment } from '../../../environment';

interface GameQuestion {
  memory: MemoryDto;
  correctAnswer: string;
  options: string[];
  albumAnswer: string | null;
  albumOptions: string[];
}

@Component({
  selector: 'app-guess-game',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './guess-game.html',
  styleUrls: ['./guess-game.css']
})
export class GuessGameComponent implements OnInit {
  @Input() memories: MemoryDto[] = [];
  @Input() members: { userId: string; name: string; role: string }[] = [];
  @Input() albums: AlbumDto[] = [];
  @Output() closed = new EventEmitter<void>();

  questions: GameQuestion[] = [];
  questionIndex = 0;
  selected: string | null = null;
  selectedAlbum: string | null = null;
  phase: 'question' | 'album' | 'result' = 'question';
  correct = 0;
  wrong = 0;
  albumMode = false;

  mediaUrl: string | null = null;
  mediaType: 'photo' | 'video' | 'quote' = 'quote';

  get current(): GameQuestion | null {
    return this.questions[this.questionIndex] ?? null;
  }

  get total(): number { return this.questions.length; }

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.generateQuestions();
    this.loadMedia();
  }

  private generateQuestions() {
    const namePool = this.buildNamePool();
    const albumNames = this.albums.map(a => a.title);

    const shuffled = [...this.memories].sort(() => Math.random() - 0.5).slice(0, 12);

    for (const memory of shuffled) {
      const correctAnswer = this.getCorrectAnswer(memory);
      const options = this.buildOptions(correctAnswer, namePool);

      let albumAnswer: string | null = null;
      let albumOptions: string[] = [];
      if (memory.happenedAt && albumNames.length >= 2) {
        const album = this.albums.find(a => {
          const mem = new Date(memory.happenedAt);
          const start = new Date(a.dateStart);
          const end = a.dateEnd ? new Date(a.dateEnd) : null;
          return mem >= start && (!end || mem <= end);
        });
        albumAnswer = album?.title ?? null;

        if (albumAnswer) {
          albumOptions = this.buildAlbumOptions(albumAnswer, albumNames);
        }
      }

      this.questions.push({ memory, correctAnswer, options, albumAnswer, albumOptions });
    }
  }

  private getCorrectAnswer(memory: MemoryDto): string {
    if (memory.type === 2) {
      return memory.quoteBy?.trim() || 'Unknown';
    }
    const people = (memory.people ?? []).filter(p => p?.trim());
    return people[0] ?? 'Unknown';
  }

  private buildNamePool(): string[] {
    const names = new Set<string>();
    for (const m of this.members) names.add(m.name);
    for (const mem of this.memories) {
      if (mem.quoteBy?.trim()) names.add(mem.quoteBy.trim());
      for (const p of (mem.people ?? [])) {
        if (p?.trim()) names.add(p.trim());
      }
    }
    names.add('Unknown');
    return Array.from(names);
  }

  private buildOptions(correct: string, pool: string[]): string[] {
    const others = pool.filter(n => n !== correct).sort(() => Math.random() - 0.5).slice(0, 3);
    const opts = [correct, ...others].sort(() => Math.random() - 0.5);
    if (opts.length < 4) {
      while (opts.length < 4) opts.push('Unknown');
    }
    return opts;
  }

  private buildAlbumOptions(correct: string, pool: string[]): string[] {
    const others = pool.filter(n => n !== correct).sort(() => Math.random() - 0.5).slice(0, 3);
    return [correct, ...others].sort(() => Math.random() - 0.5);
  }

  private loadMedia() {
    const q = this.current;
    if (!q) return;
    this.mediaUrl = null;
    const mem = q.memory;
    if (mem.type === 2) {
      this.mediaType = 'quote';
      return;
    }
    this.mediaType = mem.type === 1 ? 'video' : 'photo';
    if (!mem.mediaUrl) return;

    const fullUrl = environment.apiUrl + mem.mediaUrl;
    this.http.get(fullUrl, { responseType: 'blob' }).subscribe({
      next: (blob) => { this.mediaUrl = URL.createObjectURL(blob); },
      error: () => { this.mediaUrl = null; }
    });
  }

  select(option: string) {
    if (this.selected !== null) return;
    this.selected = option;
    if (option === this.current?.correctAnswer) {
      this.correct++;
    } else {
      this.wrong++;
    }
  }

  next() {
    const q = this.current;
    if (!q) return;

    if (this.albumMode && q.albumAnswer && q.albumOptions.length >= 2 && this.phase === 'question') {
      this.phase = 'album';
      this.selectedAlbum = null;
      return;
    }

    this.advance();
  }

  selectAlbum(option: string) {
    if (this.selectedAlbum !== null) return;
    this.selectedAlbum = option;
  }

  nextAfterAlbum() {
    this.advance();
  }

  private advance() {
    this.selected = null;
    this.selectedAlbum = null;
    this.phase = 'question';
    if (this.questionIndex + 1 >= this.total) {
      this.phase = 'result';
    } else {
      this.questionIndex++;
      this.loadMedia();
    }
  }

  restart() {
    this.questions = [];
    this.questionIndex = 0;
    this.selected = null;
    this.selectedAlbum = null;
    this.phase = 'question';
    this.correct = 0;
    this.wrong = 0;
    this.mediaUrl = null;
    this.generateQuestions();
    this.loadMedia();
  }

  optionClass(option: string): string {
    if (this.selected === null) return '';
    if (option === this.current?.correctAnswer) return 'correct';
    if (option === this.selected) return 'wrong';
    return 'dim';
  }

  albumOptionClass(option: string): string {
    if (this.selectedAlbum === null) return '';
    if (option === this.current?.albumAnswer) return 'correct';
    if (option === this.selectedAlbum) return 'wrong';
    return 'dim';
  }

  questionLabel(): string {
    const q = this.current;
    if (!q) return '';
    if (q.memory.type === 2) return 'Wer hat das gesagt?';
    return 'Wer ist in diesem Memory getaggt?';
  }
}
