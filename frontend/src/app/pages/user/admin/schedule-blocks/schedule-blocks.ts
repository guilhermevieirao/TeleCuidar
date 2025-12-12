import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableHeaderComponent } from '@shared/components/atoms/table-header/table-header';
import { BadgeComponent } from '@shared/components/atoms/badge/badge';

import { PaginationComponent } from '@shared/components/atoms/pagination/pagination';
import { SearchInputComponent } from '@shared/components/atoms/search-input/search-input';
import { FilterSelectComponent, FilterOption } from '@shared/components/atoms/filter-select/filter-select';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { ModalService } from '@core/services/modal.service';
import { WaitTimePipe } from '@core/pipes/wait-time.pipe';

@Component({
  selector: 'app-schedule-blocks',
  standalone: true,
  imports: [CommonModule, TableHeaderComponent, BadgeComponent, PaginationComponent, SearchInputComponent, FilterSelectComponent, IconComponent, WaitTimePipe],
  templateUrl: './schedule-blocks.html',
  styleUrl: './schedule-blocks.scss'
})
export class ScheduleBlocksComponent {
  blocks = [
    {
      id: 1,
      professional: { name: 'João Silva', email: 'joao@exemplo.com' },
      blockDate: { type: 'single', weekday: 'Segunda-feira', date: '10/12/2025' },
      reason: 'Consulta médica',
      status: 'pendente',
      requestDate: '09/12/2025 14:30',
      waitTime: 1, // em dias
      details: 'Solicitação para bloqueio devido a consulta médica.'
    },
    {
      id: 2,
      professional: { name: 'Maria Souza', email: 'maria@exemplo.com' },
      blockDate: { type: 'range', start: '15/12/2025', end: '18/12/2025' },
      reason: 'Férias',
      status: 'aprovada',
      requestDate: '08/12/2025 09:00',
      waitTime: 2,
      details: 'Solicitação para bloqueio por férias.',
      denialReason: 'Profissional já possui outro bloqueio aprovado para o período.'
    }
  ];

  searchTerm = '';
  statusFilter = 'all';
  statusOptions = [
    { value: 'all', label: 'Todos os status' },
    { value: 'pendente', label: 'Pendentes' },
    { value: 'aprovada', label: 'Aprovadas' },
    { value: 'negada', label: 'Negadas' },
    { value: 'expirado', label: 'Expirados' }
  ];
  sortField = 'professionalName';
  sortDirection: 'asc' | 'desc' | undefined = 'asc';
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  totalItems = 0;
  isLoading = false;

  ngOnInit(): void {
    this.loadBlocks();
  }

  onSearch(term: string): void {
    this.searchTerm = term;
    this.currentPage = 1;
    this.loadBlocks();
  }

  onStatusFilterChange(status: string): void {
    this.statusFilter = status;
    this.currentPage = 1;
    this.loadBlocks();
  }

  onSort(field: string): void {
    if (this.sortField === field) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDirection = 'asc';
    }
    this.currentPage = 1;
    this.loadBlocks();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadBlocks();
  }

  loadBlocks(): void {
    this.isLoading = true;
    // Aqui você pode integrar com o backend futuramente
    setTimeout(() => {
      // Simula carregamento
      this.isLoading = false;
      this.totalItems = this.blocks.length;
      this.totalPages = 1;
    }, 500);
  }

  constructor(private modal: ModalService) {}

  getStatusBadge(status: string): { variant: 'primary' | 'success' | 'warning' | 'error' | 'info' | 'neutral', label: string } {
    switch (status) {
      case 'pendente': return { variant: 'warning', label: 'Pendente' };
      case 'aprovada': return { variant: 'success', label: 'Aprovada' };
      case 'negada': return { variant: 'error', label: 'Negada' };
      case 'expirado': return { variant: 'neutral', label: 'Expirado' };
      default: return { variant: 'neutral', label: status };
    }
  }

  openDetails(block: any) {
    this.modal.open({
      title: 'Detalhes do Bloqueio',
      htmlMessage:
        `<div style='line-height:1.7'>`
        + `<strong>Profissional:</strong> ${block.professional.name} (${block.professional.email})<br>`
        + `<strong>Data do Bloqueio:</strong> `
        + (block.blockDate.type === 'single'
            ? `${block.blockDate.weekday} ${block.blockDate.date}`
            : `${block.blockDate.start} até ${block.blockDate.end}`) + `<br>`
        + `<strong>Motivo:</strong> ${block.reason}<br>`
        + `<strong>Status:</strong> ${this.getStatusBadge(block.status).label}<br>`
        + `<strong>Data da Solicitação:</strong> ${block.requestDate}<br>`
        + `<strong>Tempo de Espera:</strong> ${block.waitTime} dia(s)<br>`
        + `<strong>Detalhes:</strong> ${block.details}<br>`
        + (block.denialReason ? `<strong>Justificativa da Negativa:</strong> ${block.denialReason}` : '')
        + `</div>`,
      type: 'alert',
      variant: block.status === 'pendente' ? 'warning' : (block.status === 'aprovada' ? 'success' : (block.status === 'negada' ? 'danger' : undefined)),
      confirmText: 'Fechar',
      // Permite HTML no modal
    }).subscribe();
  }

  approveBlock(block: any) {
    // Aqui você pode chamar o backend futuramente
    block.status = 'aprovada';
  }

  denyBlock(block: any) {
    this.modal.prompt({
      title: 'Negar Bloqueio',
      message: 'Tem certeza que deseja negar esta solicitação de bloqueio? Por favor, forneça uma justificativa.',
      variant: 'danger',
      confirmText: 'Negar Bloqueio',
      cancelText: 'Cancelar',
      prompt: {
        label: 'Justificativa',
        placeholder: 'Ex: Conflito de agenda, etc.',
        required: true
      }
    }).subscribe(result => {
      if (result.confirmed && result.promptValue) {
        block.status = 'negada';
        block.denialReason = result.promptValue;
      }
    });
  }
}
