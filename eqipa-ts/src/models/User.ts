export enum UserStatus {
  Online = 'online',
  Offline = 'offline'
}

export enum UserPenaltyType {
  Ban = 'ban',
  Warning = 'warning',
  Blacklist = 'blacklist'
}

export interface UserPenalty {
  id?: string;
  type: UserPenaltyType;
  reason?: string;
  expires: number;
  invoker?: string;
  received: number;
}

export interface User {
  id: string;
  admin: boolean;
  status: UserStatus;
  joinedAt: number;
  firstName?: string;
  discordId?: string;
  penalties: UserPenalty[];
  lastVisit: number;
  visitCount: number;
  displayName?: string;
  lastAvatars: string[];
  currentInstanceId?: string;
}