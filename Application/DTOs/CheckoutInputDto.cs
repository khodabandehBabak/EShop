using System.ComponentModel.DataAnnotations;
using EShop.Domain.Enums;

namespace EShop.Application.DTOs;

public class CheckoutInputDto
{
    [Required(ErrorMessage = "نام گیرنده الزامی است")]
    [MaxLength(100)]
    [Display(Name = "نام و نام خانوادگی")]
    public string ReceiverName { get; set; } = "";

    [Required(ErrorMessage = "شماره موبایل الزامی است")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست")]
    [Display(Name = "شماره موبایل")]
    public string ReceiverPhone { get; set; } = "";

    [Required(ErrorMessage = "استان الزامی است")]
    [MaxLength(100)]
    [Display(Name = "استان")]
    public string Province { get; set; } = "";

    [Required(ErrorMessage = "شهر الزامی است")]
    [MaxLength(100)]
    [Display(Name = "شهر")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "کد پستی الزامی است")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "کد پستی باید ۱۰ رقم باشد")]
    [Display(Name = "کد پستی")]
    public string PostalCode { get; set; } = "";

    [Required(ErrorMessage = "آدرس الزامی است")]
    [MaxLength(500)]
    [Display(Name = "آدرس کامل")]
    public string FullAddress { get; set; } = "";

    [MaxLength(500)]
    [Display(Name = "توضیحات سفارش")]
    public string? Note { get; set; }

    [Required(ErrorMessage = "روش ارسال را انتخاب کنید")]
    [Display(Name = "روش ارسال")]
    public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Normal;

    [Required(ErrorMessage = "روش پرداخت را انتخاب کنید")]
    [Display(Name = "روش پرداخت")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;

    [MaxLength(50)]
    [Display(Name = "کد تخفیف")]
    public string? CouponCode { get; set; }

    public bool SaveAddress { get; set; } = true;
}