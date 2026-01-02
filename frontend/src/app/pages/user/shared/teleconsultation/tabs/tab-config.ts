import type { IconName } from '@shared/components/atoms/icon/icon';

export interface TabConfig {
  id: string;
  label: string;
  icon: IconName;
  roles: ('PATIENT' | 'PROFESSIONAL' | 'ADMIN')[];
  /** Se a tab deve aparecer na teleconsulta (modo de atendimento) */
  showInTeleconsultation: boolean;
  /** Se a tab deve aparecer nos detalhes da consulta (modo de visualização) */
  showInDetails: boolean;
  /** Ordem de exibição */
  order: number;
}

/**
 * Configuração centralizada de todas as tabs disponíveis.
 * 
 * IMPORTANTE - PADRÃO DE CONFIGURAÇÃO:
 * =====================================
 * 
 * 1. ADIÇÃO DE NOVAS TABS:
 *    - Para adicionar uma nova tab, basta adicioná-la a este array TELECONSULTATION_TABS
 *    - Configure `showInTeleconsultation: true` se deve aparecer na videochamada
 *    - Configure `showInDetails: true` se deve aparecer na tela de detalhes
 *    - A tab será automaticamente incluída em ambas as telas conforme configuração
 * 
 * 2. MODO READONLY AUTOMÁTICO:
 *    - Todas as tabs na tela de detalhes (/consultas/:id/detalhes) são AUTOMATICAMENTE
 *      configuradas como somente leitura através da propriedade `isDetailsView`
 *    - NÃO é necessário adicionar readonly manualmente em cada tab
 *    - O componente pai (AppointmentDetailsComponent) gerencia isso globalmente
 * 
 * 3. CNS É A ÚNICA EXCEÇÃO:
 *    - CNS tem `showInDetails: false` porque é específica do atendimento ao vivo
 *    - Todas as outras tabs seguem o padrão: aparecem em ambas as telas
 * 
 * 4. COMO FUNCIONA:
 *    - getTeleconsultationTabs(): retorna tabs para teleconsulta (showInTeleconsultation: true)
 *    - getDetailsTabs(): retorna tabs para detalhes (showInDetails: true)
 *    - Novas tabs são automaticamente incluídas baseado nessas flags
 */
export const TELECONSULTATION_TABS: TabConfig[] = [
  {
    id: 'basic',
    label: 'Informações Básicas',
    icon: 'file',
    roles: ['PATIENT', 'PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: false, // Não mostra na teleconsulta, apenas nos detalhes
    showInDetails: true,
    order: 0
  },
  {
    id: 'patient-data',
    label: 'Dados do Paciente',
    icon: 'user',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 1
  },
  {
    id: 'pre-consultation',
    label: 'Dados da Pré Consulta',
    icon: 'file',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: false, // Temporariamente desativado
    showInDetails: false, // Temporariamente desativado
    order: 2
  },
  {
    id: 'anamnesis',
    label: 'Anamnese',
    icon: 'book',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 3
  },
  {
    id: 'specialty',
    label: 'Campos da Especialidade',
    icon: 'stethoscope',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 4
  },
  {
    id: 'iot',
    label: 'IOT',
    icon: 'activity',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: false, // Aparece apenas nos detalhes
    showInDetails: true,
    order: 5
  },
  {
    id: 'biometrics',
    label: 'Biométricos',
    icon: 'heart',
    roles: ['PATIENT', 'PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 6
  },
  {
    id: 'attachments',
    label: 'Chat Anexos',
    icon: 'camera',
    roles: ['PATIENT', 'PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 7
  },
  {
    id: 'soap',
    label: 'SOAP',
    icon: 'book',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 8
  },
  {
    id: 'receita',
    label: 'Receita',
    icon: 'file',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 9
  },
  {
    id: 'atestado',
    label: 'Atestado',
    icon: 'file',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 10
  },
  {
    id: 'exame',
    label: 'Exame',
    icon: 'file',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 11
  },
  {
    id: 'laudo',
    label: 'Laudo',
    icon: 'file',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 12
  },
  {
    id: 'ai',
    label: 'IA',
    icon: 'activity',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 13
  },
  {
    id: 'cns',
    label: 'CNS',
    icon: 'user',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: false, // CNS NÃO aparece nos detalhes (apenas na teleconsulta)
    order: 14
  },
  {
    id: 'return',
    label: 'Retorno',
    icon: 'calendar',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 15
  },
  {
    id: 'referral',
    label: 'Encaminhamento',
    icon: 'arrow-right',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 16
  },
  {
    id: 'conclusion',
    label: 'Concluir',
    icon: 'check',
    roles: ['PROFESSIONAL', 'ADMIN'],
    showInTeleconsultation: true,
    showInDetails: true, // Aparece automaticamente nos detalhes
    order: 17
  }
];

/**
 * Retorna as tabs disponíveis para a teleconsulta, filtradas por role
 */
export function getTeleconsultationTabs(role: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN'): TabConfig[] {
  return TELECONSULTATION_TABS
    .filter(tab => tab.showInTeleconsultation && tab.roles.includes(role))
    .sort((a, b) => a.order - b.order);
}

/**
 * Retorna as tabs disponíveis para a página de detalhes, filtradas por role
 */
export function getDetailsTabs(role: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN'): TabConfig[] {
  return TELECONSULTATION_TABS
    .filter(tab => tab.showInDetails && tab.roles.includes(role))
    .sort((a, b) => a.order - b.order);
}

/**
 * Retorna todas as tabs disponíveis para a página de detalhes (sem filtro de role)
 */
export function getAllDetailsTabs(): TabConfig[] {
  return TELECONSULTATION_TABS
    .filter(tab => tab.showInDetails)
    .sort((a, b) => a.order - b.order);
}

/**
 * Mapeamento de id da tab para o nome usado na teleconsulta antiga
 */
export const TAB_ID_TO_LEGACY_NAME: Record<string, string> = {
  'patient-data': 'Dados do Paciente',
  'pre-consultation': 'Dados da Pré Consulta',
  'anamnesis': 'Anamnese',
  'specialty': 'Campos da Especialidade',
  'biometrics': 'Biométricos',
  'attachments': 'Chat Anexos',
  'soap': 'SOAP',
  'receita': 'Receita',
  'atestado': 'Atestado',
  'exame': 'Exame',
  'laudo': 'Laudo',
  'ai': 'IA',
  'cns': 'CNS',
  'return': 'Retorno',
  'referral': 'Encaminhamento',
  'conclusion': 'Concluir'
};

/**
 * Mapeamento inverso: nome legacy para id
 */
export const LEGACY_NAME_TO_TAB_ID: Record<string, string> = Object.fromEntries(
  Object.entries(TAB_ID_TO_LEGACY_NAME).map(([id, name]) => [name, id])
);
