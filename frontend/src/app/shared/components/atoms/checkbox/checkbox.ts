import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { IconComponent, IconName } from '../icon/icon';

@Component({
  selector: 'app-checkbox',
  standalone: true,
  imports: [CommonModule, IconComponent],
  template: `
    <label class="checkbox" [class.checkbox--disabled]="disabled" [class.checkbox--with-icon]="icon">
      <input
        type="checkbox"
        class="checkbox__input"
        [checked]="value"
        [disabled]="disabled"
        (change)="onChange($event)"
        (blur)="onTouched()"
      />
      <span class="checkbox__box">
        @if (icon) {
          <app-icon [name]="icon" [size]="14" class="checkbox__icon" />
        }
      </span>
      <span class="checkbox__label" *ngIf="label">{{ label }}</span>
    </label>
  `,
  styles: [`
    @use 'variables' as *;
    @use 'mixins' as *;

    .checkbox {
      display: inline-flex;
      align-items: center;
      gap: $spacing-sm;
      cursor: pointer;
      user-select: none;

      &--disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      &__input {
        position: absolute;
        opacity: 0;
        pointer-events: none;

        &:checked + .checkbox__box {
          background: var(--primary-600);
          border-color: var(--primary-600);

          &::after {
            opacity: 1;
            transform: scale(1);
          }
        }

        &:focus + .checkbox__box {
          box-shadow: 0 0 0 3px rgba(var(--primary-rgb), 0.1);
        }
      }

      &__box {
        position: relative;
        display: flex;
        align-items: center;
        justify-content: center;
        width: 20px;
        height: 20px;
        border: 2px solid var(--border-color);
        border-radius: $radius-sm;
        background: var(--surface-primary);
        @include transition(all);

        &::after {
          content: '';
          position: absolute;
          top: 2px;
          left: 5px;
          width: 6px;
          height: 10px;
          border: solid white;
          border-width: 0 2px 2px 0;
          transform: rotate(45deg) scale(0);
          opacity: 0;
          @include transition(all);
        }
      }

      &--with-icon {
        .checkbox__input:checked + .checkbox__box {
          &::after {
            display: none;
          }
        }

        .checkbox__icon {
          opacity: 0;
          transform: scale(0);
          @include transition(all);
          color: white;
        }

        .checkbox__input:checked + .checkbox__box .checkbox__icon {
          opacity: 1;
          transform: scale(1);
        }
      }

      &__label {
        font-size: $font-size-sm;
        color: var(--text-secondary);
        line-height: 1.4;
      }

      &:hover:not(&--disabled) .checkbox__box {
        border-color: var(--primary-500);
      }
    }
  `],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CheckboxComponent),
      multi: true
    }
  ]
})
export class CheckboxComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() disabled = false;
  @Input() icon?: IconName;

  value = false;
  
  onChangeCallback: any = () => {};
  onTouched: any = () => {};

  onChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.value = target.checked;
    this.onChangeCallback(this.value);
  }

  writeValue(value: boolean): void {
    this.value = value;
  }

  registerOnChange(fn: any): void {
    this.onChangeCallback = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
