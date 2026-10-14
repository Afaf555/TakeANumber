export type Service = {
  id: number;
  name: string;
};

export type Ticket = {
  id: number;
  number: number;
  serviceId: number;
  serviceName: string;
  status: string;
  createdAt: string;
  peopleAhead: number;
  counterName: string | null;
};