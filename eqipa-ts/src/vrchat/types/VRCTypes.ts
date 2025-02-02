export interface VRCInstance {
  processId: number;
  groupId: string;
  worldId: string;
  worldName: string;
  worldType: string;
  worldRegion: string;
}

export interface VRCModeration {
  type: 'warn' | 'kick';
  reason?: string;
  worldId?: string;
  expires: '1_hour_ahead' | '1_day_ahead' | '';
  created: string;
  isPermanent: boolean;
  targetUserId?: string;
  instanceId?: string;
}

export interface VRCLogEvent {
  action: string;
  data: Record<string, any>;
}