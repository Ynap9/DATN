import { Utils } from "../../utils";
export class Page {

    constructor(moduleCall?: string) {
    }

    pageSizeAll = 9999999;
    isInit: boolean = true;

    perPageShowPaginatorInTab = 25;
    perPageOptions: number[] = [25, 50, 100, 200];
    pageSizeNotify: number= 25;
    pageNumberFirst: number = 1;
    pageSize: number = this.perPageOptions[0];
    pageSizeMin: number = this.perPageOptions[0];
    totalItems: number = 0;
    totalPages: number = 0;
    pageNumber: number = 0;
    pageNumberLoadMore: number = 0;
    pageSizeLoadMore: number = 100;
    dataLinkPrev: string;

    keyword: string = '';

    isActive: boolean | string;

    previousFilterValue: any;

	pageNumberOld: number = 0;

    getPageNumber(dataFilter: any = {}) {
		const changePageNumber = this.pageNumberOld !== this.pageNumber;

        const currentFilterValue = dataFilter;
        if((dataFilter && !Utils.compareData(this.previousFilterValue, dataFilter) && !changePageNumber) || this.isInit) {
            this.isInit = false;
            this.previousFilterValue = Utils.cloneData(dataFilter);
            this.resetPage();
        }
        let currentPage = this.pageNumber;
        if(this.pageSize === this.pageSizeAll) {
            currentPage = this.pageNumberLoadMore;
            this.pageNumberLoadMore++;
        } else {
			this.pageNumberLoadMore = 0;
        }
		this.pageNumberOld = currentPage;
        return currentPage + 1;
    }

    resetPage() {
      this.pageNumber = 0;
      this.pageNumberLoadMore = 0;
    }

    getPageSize() {
        return (this.pageSize !== this.pageSizeAll) ? this.pageSize : this.pageSizeLoadMore;
    }

    getRowLoadMore(rows, responseData) {
        const pageNumberStart = 1;
        responseData = responseData || [];
        if(this.pageSize === this.pageSizeAll) {
            if(this.pageNumberLoadMore === pageNumberStart) return responseData;
            return [...rows, ...responseData];
        } else {
            return responseData;
        }
    }

    setDataLinkPrev(dataLink: string) {
        if(dataLink !== this.dataLinkPrev) {
            this.pageNumberLoadMore = 0;
            this.pageNumber = 0;
            this.dataLinkPrev = dataLink;
        }
    }
}
