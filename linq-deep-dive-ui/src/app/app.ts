import { HttpClient } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';

@Component({
  selector: 'app-root',
  imports: [DecimalPipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly http = inject(HttpClient);
  readonly loading = signal(false);
  readonly actionLoading = signal(false);
  readonly error = signal('');
  readonly data = signal<LinqDemoResult | null>(null);
  readonly selectedConcept = signal(0);
  readonly actionResult = signal<ConceptActionResult | null>(null);
  readonly activeMistakeId = signal('');

  ngOnInit(): void {
    this.runDemo();
  }

  runDemo(): void {
    this.loading.set(true);
    this.error.set('');
    this.http.get<LinqDemoResult>('http://localhost:5221/api/linqdemo/run').subscribe({
      next: result => {
        this.data.set(result);
        this.selectedConcept.set(0);
        this.actionResult.set(null);
        this.activeMistakeId.set('');
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not connect to API on http://localhost:5221. Run backend project first.');
        this.loading.set(false);
      }
    });
  }

  selectConcept(index: number): void {
    this.selectedConcept.set(index);
    this.actionResult.set(null);
    this.activeMistakeId.set('');
  }

  runExample(): void {
    const current = this.data()?.concepts[this.selectedConcept()];
    if (!current) {
      return;
    }
    this.activeMistakeId.set('');
    this.executeAction(current.id, 'run-real-example');
  }

  runMistakeExample(mistakeId: string): void {
    const current = this.data()?.concepts[this.selectedConcept()];
    if (!current) {
      return;
    }
    this.activeMistakeId.set(mistakeId);
    this.executeAction(current.id, mistakeId);
  }

  runTermExample(actionId: string): void {
    const current = this.data()?.concepts[this.selectedConcept()];
    if (!current || !actionId) {
      return;
    }
    this.activeMistakeId.set(actionId);
    this.executeAction(current.id, actionId);
  }

  barWidth(ms: number, comparisons: ComparisonItem[]): number {
    const max = Math.max(...comparisons.map(c => c.milliseconds), 1);
    return ms === 0 ? 4 : Math.max(4, (100 * ms) / max);
  }

  private executeAction(conceptId: string, action: string): void {
    this.actionLoading.set(true);
    const encodedConcept = encodeURIComponent(conceptId);
    const encodedAction = encodeURIComponent(action);
    this.http
      .get<ConceptActionResult>(
        `http://localhost:5221/api/linqdemo/action?conceptId=${encodedConcept}&name=${encodedAction}`
      )
      .subscribe({
        next: result => {
          this.actionResult.set(result);
          this.actionLoading.set(false);
        },
        error: () => {
          this.error.set('Failed to execute concept action from backend API.');
          this.actionLoading.set(false);
        }
      });
  }
}

type LinqDemoResult = {
  concepts: LinqConcept[];
  iEnumerableVsIQueryable: {
    iEnumerableMilliseconds: number;
    iEnumerableRowsLoaded: number;
    iQueryableMilliseconds: number;
    iQueryableRowsLoaded: number;
  };
  deferredVsImmediate: {
    deferredBeforeCount: number;
    deferredAfterCount: number;
    immediateListCount: number;
  };
  sqlTranslation: {
    sql: string;
  };
  filterProjectionChaining: {
    orderId: number;
    customerName: string;
    total: number;
    category: string;
  }[];
  commonMistakes: {
    earlyToListMilliseconds: number;
    sqlFilterMilliseconds: number;
    trackingMilliseconds: number;
    noTrackingMilliseconds: number;
  };
};

type LinqConcept = {
  id: string;
  title: string;
  category: string;
  definition: string;
  overview: string;
  termDefinitions: TermDefinition[];
  demoCode: string;
  codeExamples: CodeExample[];
  presentationSlides: PresentationSlide[];
  mistakeExamples: MistakeExample[];
};

type PresentationSlide = {
  title: string;
  intro: string;
  exampleCode: string;
  internalSteps: string[];
  sqlOutput: string;
  badExampleCode: string;
  failureExplanation: string;
};

type TermDefinition = {
  term: string;
  definition: string;
  exampleCode: string;
  badExampleCode: string;
  goodExampleCode: string;
  actionId: string;
};

type CodeExample = {
  title: string;
  cSharpCode: string;
};

type MistakeExample = {
  id: string;
  title: string;
  description: string;
  cSharpCode: string;
};

type ComparisonItem = {
  label: string;
  milliseconds: number;
  records: number;
};

type ConceptActionResult = {
  conceptId: string;
  action: string;
  summary: string;
  recordsReturned: number;
  executionMilliseconds: number;
  estimatedMemoryKb: number;
  performanceRating: string;
  comparisons: ComparisonItem[];
};
