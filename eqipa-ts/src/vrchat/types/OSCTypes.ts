export interface OSCMessage {
  address: string;
  args: any[];
}

export enum OSCAddresses {
  CHATBOX_TYPING = '/chatbox/typing',
  CHATBOX_INPUT = '/chatbox/input',
}

export interface OSCEndpoint {
  address: string;
  port: number;
}