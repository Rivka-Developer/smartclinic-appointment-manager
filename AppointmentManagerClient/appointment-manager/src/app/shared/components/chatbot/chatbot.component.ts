import {
  Component,
  inject,
  signal,
  computed,
  ElementRef,
  ViewChild,
  ChangeDetectionStrategy,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ChatbotService } from '../../../core/services/chatbot.service';
import { ChatMessage } from '../../../core/models';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-chatbot',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.css'],
})
export class ChatbotComponent {
  private auth = inject(AuthService);
  private chatbotService = inject(ChatbotService);

  @ViewChild('messagesEl') private messagesEl!: ElementRef<HTMLDivElement>;

  readonly isOpen = signal(false);
  readonly loading = signal(false);
  userInput = '';

  readonly messages = signal<ChatMessage[]>([
    { role: 'assistant', content: 'שלום! אני העוזר הווירטואלי של SmartClinic 👋\nאיך אוכל לעזור לך היום?' },
  ]);

  /** מוצג רק ללקוחות מחוברים (לא למנהל) */
  readonly isVisible = computed(() => this.auth.isLoggedIn() && !this.auth.isAdmin());

  toggle(): void {
    this.isOpen.update((v) => !v);
  }

  send(): void {
    const message = this.userInput.trim();
    if (!message || this.loading()) return;

    const history = [...this.messages()];
    this.userInput = '';
    this.messages.update((msgs) => [...msgs, { role: 'user', content: message }]);
    this.loading.set(true);
    this.scrollToBottom();

    this.chatbotService.send({ message, history }).subscribe({
      next: (res) => {
        this.messages.update((msgs) => [...msgs, { role: 'assistant', content: res.reply }]);
        this.loading.set(false);
        this.scrollToBottom();
      },
      error: () => {
        this.messages.update((msgs) => [
          ...msgs,
          { role: 'assistant', content: 'מצטער, אירעה שגיאה. אנא נסה שנית.' },
        ]);
        this.loading.set(false);
        this.scrollToBottom();
      },
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.messagesEl?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 60);
  }
}
